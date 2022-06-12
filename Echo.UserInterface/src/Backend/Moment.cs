using System;
using System.Diagnostics;

namespace Echo.UserInterface.Backend;

/// <summary>
/// A small container for time related variables.
/// </summary>
public readonly struct Moment
{
	public Moment(in Moment previous, Stopwatch stopwatch)
	{
		elapsed = stopwatch.Elapsed;
		delta = elapsed - previous.elapsed;
		frame = previous.frame + 1;
	}

	/// <summary>
	/// The time since the application was initialized.
	/// </summary>
	public readonly TimeSpan elapsed;

	/// <summary>
	/// The time since the last frame that was drawn.
	/// </summary>
	public readonly TimeSpan delta;

	/// <summary>
	/// The frame count of the current frame.
	/// </summary>
	public readonly ulong frame;
}