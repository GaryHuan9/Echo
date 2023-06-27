using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Echo.Core.Common;
using Echo.Core.Common.Diagnostics;

namespace Echo.UserInterface.Core;

public static class ActionQueue
{
	static readonly BlockingCollection<(string label, Action action)> queue = new();
	static readonly HashSet<string> labels = new();

	static Thread thread;

	static MonoThread monoThread;

	public static void Enqueue(string label, Action action)
	{
		monoThread.Ensure();

		lock (labels)
		{
			if (!labels.Add(label))
			{
				LogList.Add($"Attempting to enqueue duplicate '{label}' into {nameof(ActionQueue)}.");
				return;
			}

			LogList.Add($"Successfully enqueued '{label}' into {nameof(ActionQueue)}.");
			queue.Add((label, action));
		}

		if (thread == null) LaunchThread();
	}

	static void ProcessQueue()
	{
		foreach ((string label, Action action) in queue.GetConsumingEnumerable())
		{
			LogList.Add($"Dequeued and began executing '{label}' from {nameof(ActionQueue)}.");

			action();

			LogList.Add($"Completed executing '{label}' from {nameof(ActionQueue)}.");

			lock (labels)
			{
				// ReSharper disable once RedundantAssignment
				bool removed = labels.Remove(label);
				Ensure.IsTrue(removed);
			}
		}
	}

	static void LaunchThread()
	{
		thread = new Thread(ProcessQueue) { IsBackground = true };
		thread.Start();
	}
}