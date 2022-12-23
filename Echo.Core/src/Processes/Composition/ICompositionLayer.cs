using Echo.Core.Common.Compute.Async;

namespace Echo.Core.Processes.Composition;

public interface ICompositionLayer
{
	public ComputeTask ExecuteAsync(CompositeContext context);
}