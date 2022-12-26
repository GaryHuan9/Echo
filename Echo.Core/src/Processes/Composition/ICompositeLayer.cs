using Echo.Core.Common.Compute.Async;

namespace Echo.Core.Processes.Composition;

public interface ICompositeLayer
{
	public ComputeTask ExecuteAsync(CompositeContext context);
}