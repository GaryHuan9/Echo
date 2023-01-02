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
	/// <exception cref="ICompositeContext.TextureNotFoundException">Can be throw if a required texture is missing.</exception>
	public ComputeTask ExecuteAsync(ICompositeContext context);
}