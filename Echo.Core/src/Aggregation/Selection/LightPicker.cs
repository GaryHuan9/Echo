using Echo.Core.Aggregation.Bounds;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Sampling;

namespace Echo.Core.Aggregation.Selection;

/// <summary>
/// An aggregate with a strategy of selecting lights from a <see cref="LightCollection"/>.
/// </summary>
public abstract class LightPicker
{
	BoxBound? _boxBound;

	/// <summary>
	/// The <see cref="Bounds.ConeBound"/> that bounds all of the directions of this <see cref="LightPicker"/>.
	/// </summary>
	public abstract ConeBound ConeBound { get; }

	/// <summary>
	/// The <see cref="Bounds.BoxBound"/> that bounds all light in this <see cref="LightPicker"/>.
	/// </summary>
	public BoxBound BoxBound
	{
		get
		{
			_boxBound ??= GetTransformedBound(Float4x4.identity);
			return _boxBound.Value;
		}
	}

	/// <summary>
	/// The total emissive power of this <see cref="LightPicker"/>.
	/// </summary>
	public abstract float Power { get; }

	/// <summary>
	/// Calculates a <see cref="BoxBound"/> that bounds this <see cref="LightPicker"/> while transformed.
	/// </summary>
	/// <param name="transform">The <see cref="Float4x4"/> used to transform this <see cref="LightPicker"/>.</param>
	public abstract BoxBound GetTransformedBound(in Float4x4 transform);

	/// <summary>
	/// Selects a light using this <see cref="LightPicker"/>.
	/// </summary>
	/// <param name="origin">The <see cref="GeometryPoint"/> from which this light should be selected based off of.</param>
	/// <param name="sample">The <see cref="Sample1D"/> value used for this selection. This value should be modified
	/// using <see cref="Sample1D.Stretch"/> to ensure that the result after this value is still uniform and unbiased.</param>
	/// <returns>The selected light represented by am <see cref="EntityToken"/>.</returns>
	public abstract Probable<EntityToken> Pick(in GeometryPoint origin, ref Sample1D sample);

	/// <summary>
	/// Returns the probability mass function (pmf) value of selecting a light using this <see cref="LightPicker"/>.
	/// </summary>
	/// <param name="token">An <see cref="EntityToken"/> that represents the light that was selected.</param>
	/// <param name="origin">The <see cref="GeometryPoint"/> from which the selection was based off of.</param>
	/// <returns>The calculated pmf value.</returns>
	public abstract float ProbabilityMass(EntityToken token, in GeometryPoint origin);
}