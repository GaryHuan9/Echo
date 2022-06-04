using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using Echo.Common;

namespace Echo.UserInterface.Core;

public static class ActionQueue
{
	static readonly BlockingCollection<Labeled<Action>> queue = new();
	static ConcurrentList<Labeled<Record>> history = new();

	static readonly HashSet<string> labels = new();
	static readonly Locker locker = new();
	static bool hasThread;

	public static IReadOnlyList<Labeled<Record>> History => history;

	public static void Enqueue(Action action, string label)
	{
		using var _ = locker.Fetch();

		if (!labels.Add(label))
		{
			AddRecord(label, EventType.EnqueuedDuplicate);
			return;
		}

		AddRecord(label, EventType.EnqueueSucceed);
		queue.Add(new Labeled<Action>(label, action));

		if (hasThread) return;

		var thread = new Thread(ProcessQueue) { IsBackground = true };
		hasThread = true;
		thread.Start();
	}

	public static void ClearHistory() => history = new ConcurrentList<Labeled<Record>>();

	static void ProcessQueue()
	{
		foreach ((string label, Action action) in queue.GetConsumingEnumerable())
		{
			AddRecord(label, EventType.DequeueStarted);

			action();

			AddRecord(label, EventType.DequeueCompleted);

			bool removed = labels.Remove(label);
			Assert.IsTrue(removed);
		}
	}

	static void AddRecord(string label, EventType type) => history.ImmediateAdd(new Labeled<Record>(label, new Record(DateTime.Now, type)));

	public enum EventType
	{
		EnqueueSucceed,
		EnqueuedDuplicate,
		DequeueStarted,
		DequeueCompleted
	}

	public readonly record struct Labeled<T>(string Label, in T Item);
	public readonly record struct Record(DateTime Time, EventType Type);
}