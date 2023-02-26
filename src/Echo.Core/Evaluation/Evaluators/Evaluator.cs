using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
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
	/// <param name="texture">The destination containing <see cref="RenderTexture"/>.</param>
	/// <param name="label">The <see cref="string"/> used to identify the layer.</param>
	/// <returns>The <see cref="IEvaluationLayer"/> for this new evaluation session.</returns>
	/// <remarks> A simple invocation to static method <see cref="CreateOrClearLayer{T}"/> with a specified type
	/// parameter is sufficient for most implementations that require a custom <see cref="IColor{T}"/> type. </remarks>
	public virtual IEvaluationLayer CreateOrClearLayer(RenderTexture texture, string label) => CreateOrClearLayer<RGB128>(texture, label);

	/// <summary>
	/// Evaluates a <see cref="PreparedScene"/> using this <see cref="Evaluator"/>.
	/// </summary>
	/// <param name="scene">The <see cref="PreparedScene"/> to evaluate.</param>
	/// <param name="ray">The originating <see cref="Ray"/> where the entire evaluation begins.</param>
	/// <param name="distribution">Generator to create any <see cref="Sample1D"/> or <see cref="Sample2D"/>.</param>
	/// <param name="allocator">Memory allocator available to be used by this method.</param>
	/// <param name="statistics">Access to an <see cref="EvaluatorStatistics"/> to report various evaluation related values.</param>
	/// <remarks>The implementation do not need to invoke <see cref="Allocator.Release"/> before this method returns.</remarks>
	public abstract Float4 Evaluate(PreparedScene scene, in Ray ray, ContinuousDistribution distribution, Allocator allocator, ref EvaluatorStatistics statistics);

	/// <summary>
	/// Default implementation for <see cref="CreateOrClearLayer"/>.
	/// </summary>
	/// <param name="texture">The destination <see cref="RenderTexture"/>.</param>
	/// <param name="label">The <see cref="string"/> used to identify the layer.</param>
	/// <typeparam name="T">The <see cref="IColor{T}"/> type for this layer.</typeparam>
	/// <returns>The <see cref="IEvaluationLayer"/> that was found or created.</returns>
	protected static IEvaluationLayer CreateOrClearLayer<T>(RenderTexture texture, string label) where T : unmanaged, IColor<T>
	{
		bool found = texture.TryGetLayer<T, EvaluationLayer<T>>(label, out var layer);
		if (!found) return texture.CreateLayer<T>(label);

		layer.Clear();
		return layer;
	}
}