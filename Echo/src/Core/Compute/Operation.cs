using System.Collections.Generic;

namespace Echo.Core.Compute;

public abstract class Operation
{
	public abstract void Validate();

	public abstract bool Execute(Scheduler scheduler);
}

public abstract class Operation<T> : Operation
{
	readonly Queue<T> queue = new(QueueSize);

	const int QueueSize = 64;

	public sealed override bool Execute(Scheduler scheduler)
	{
		if (!TryGetPayload(out T payload)) return false;

		Main(payload, scheduler);
		return true;
	}

	protected abstract bool NextPayload(out T payload);

	protected abstract void Main(in T payload, Scheduler scheduler);

	bool TryGetPayload(out T payload)
	{
		lock (queue)
		{
			if (queue.TryDequeue(out payload)) return true;

			while (queue.Count < QueueSize && NextPayload(out payload)) queue.Enqueue(payload);

			return queue.TryDequeue(out payload);
		}
	}
}