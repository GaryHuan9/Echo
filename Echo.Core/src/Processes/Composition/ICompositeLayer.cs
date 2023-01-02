using System;
using Echo.Core.Common.Compute.Async;

namespace Echo.Core.Processes.Composition;

/// <summary>
/// A layer of computation in a <see cref="CompositionOperation"/>.
/// </summary>
public interface ICompositeLayer
{
	/// <summary>
	/// Asynchronously executes this layer; returns a <see cref="ComputeTask"/> for the computation.
	/// </summary>
	/// <exception cref="CompositeException">Can be throw if an error occurs.</exception>
	public ComputeTask ExecuteAsync(ICompositeContext context);

	/// <summary>
	/// <see cref="Exception"/> thrown when a <see cref="ICompositeLayer"/> cannot complete its task.
	/// </summary>
	/// <remarks>Caught by <see cref="CompositionOperation"/> as error messages.</remarks>
	public class CompositeException : Exception
	{
		public CompositeException(string message) : base(message) { }
	}
}