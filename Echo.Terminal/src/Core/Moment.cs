using System;
using System.Diagnostics;

namespace Echo.Terminal.Core;

public readonly struct Moment
{
	public Moment(in Moment previous, Stopwatch stopwatch)
	{
		elapsed = stopwatch.Elapsed;
		delta = elapsed - previous.elapsed;
	}

	public readonly TimeSpan elapsed;
	public readonly TimeSpan delta;
}