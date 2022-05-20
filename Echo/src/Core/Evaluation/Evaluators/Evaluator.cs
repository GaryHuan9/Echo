using CodeHelpers;
using CodeHelpers.Packed;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Memory;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Grid;

namespace Echo.Core.Evaluation.Evaluators;

public abstract record Evaluator
{
	readonly NotNull<string> _destination = "main";

	/// <summary>
	/// The label of the destination buffer in the <see cref="RenderBuffer"/> to write to.
	/// </summary>
	public string Destination
	{
		get => _destination;
		init => _destination = value;
	}

	/// <summary>
	/// Evaluates a <see cref="PreparedScene"/> using this <see cref="Evaluator"/>.
	/// </summary>
	/// <param name="scene">The <see cref="PreparedScene"/> to evaluate.</param>
	/// <param name="ray">The originating <see cref="Ray"/> where the entire evaluation begins.</param>
	/// <param name="distribution">Generator to create any <see cref="Sample1D"/> or <see cref="Sample2D"/>.</param>
	/// <param name="allocator">Memory allocator available to be used by this method.</param>
	/// <remarks>The implementation do not need to invoke <see cref="Allocator.Release"/> before this method returns.</remarks>
	public abstract Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator);
}