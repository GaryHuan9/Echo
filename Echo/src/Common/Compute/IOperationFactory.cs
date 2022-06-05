using System.Collections.Immutable;

namespace Echo.Common.Compute;

/// <summary>
/// A factory used by <see cref="Device"/> to create new <see cref="Operation"/>s.
/// </summary>
public interface IOperationFactory
{
	/// <summary>
	/// Creates a new <see cref="Operation"/> to be worked by <paramref name="workers"/>.
	/// </summary>
	/// <param name="workers">The <see cref="IWorker"/>s that will execute this new <see cref="Operation"/>.</param>
	/// <returns>The new <see cref="Operation"/> that was created.</returns>
	public Operation CreateOperation(ImmutableArray<IWorker> workers);
}