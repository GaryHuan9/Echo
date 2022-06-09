using CodeHelpers;
using CodeHelpers.Packed;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Memory;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Evaluation.Evaluators;

public abstract record Evaluator
{
	/// <summary>
	/// Creates or cleans an <see cref="IEvaluationLayer"/> for a new evaluation session.
	/// </summary>
	/// <param name="buffer">The destination containing <see cref="RenderBuffer"/>.</param>
	/// <returns>The <see cref="IEvaluationLayer"/> for this new evaluation session.</returns>
	public abstract IEvaluationLayer CreateOrClearLayer(RenderBuffer buffer);

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