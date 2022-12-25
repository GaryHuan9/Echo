using Echo.Core.Common.Compute.Async;

namespace Echo.Core.Processes.Composition;

public interface ICompositionLayer
{
	public ComputeTask ExecuteAsync(CompositionContext context);
}