using CodeHelpers.Packed;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Evaluation.Evaluators;

/// <summary>
/// An immutable object with the ability to evaluate a <see cref="PreparedScene"/> through <see cref="Ray"/>s.
/// </summary>
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

	/// <summary>
	/// Default implementation for <see cref="CreateOrClearLayer"/>.
	/// </summary>
	/// <param name="buffer">The destination <see cref="RenderBuffer"/>.</param>
	/// <param name="label">The <see cref="string"/> layer label to use.</param>
	/// <typeparam name="T">The <see cref="IColor{T}"/> type for this layer.</typeparam>
	/// <returns>The <see cref="IEvaluationLayer"/> that was found or created.</returns>
	protected static IEvaluationLayer CreateOrClearLayer<T>(RenderBuffer buffer, string label) where T : unmanaged, IColor<T>
	{
		bool found = buffer.TryGetTexture<T, EvaluationLayer<T>>(label, out var layer);
		if (!found) return buffer.CreateLayer<T>(label);

		layer.Clear();
		return layer;
	}
}