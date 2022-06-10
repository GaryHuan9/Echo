using CodeHelpers.Packed;
using Echo.Common.Mathematics.Primitives;
using Echo.Common.Memory;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Evaluation;

namespace Echo.Core.Evaluation.Evaluators;

public abstract record Evaluator
{
	/// <summary>
	/// Creates or cleans an <see cref="ITiledEvaluationLayer"/> for a new evaluation session.
	/// </summary>
	/// <param name="buffer">The destination containing <see cref="RenderBuffer"/>.</param>
	/// <returns>The <see cref="ITiledEvaluationLayer"/> for this new evaluation session.</returns>
	public abstract ITiledEvaluationLayer CreateOrClearLayer(RenderBuffer buffer);

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
	/// <returns>The <see cref="ITiledEvaluationLayer"/> that was found or created.</returns>
	protected static ITiledEvaluationLayer CreateOrClearLayer<T>(RenderBuffer buffer, string label) where T : unmanaged, IColor<T>
	{
		bool found = buffer.TryGetTexture<T, TiledEvaluationLayer<T>>(label, out var layer);
		if (!found) return buffer.CreateLayer<T>(label);

		layer.Clear();
		return layer;
	}
}