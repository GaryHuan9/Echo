using System;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

/// <summary>
/// The base class for either a bidirectional reflectance or transmittance distribution function.
/// NOTE: unless specifically indicated, all directions in this class is in local-space, meaning
/// <see cref="Float3.Forward"/> is the surface normal at our point of interest, and incident and
/// outgoing unit directions point away from that point.
/// </summary>
public abstract class BxDF
{
	protected BxDF(FunctionType type) => this.type = type;

	public readonly FunctionType type;

	/// <summary>
	/// Evaluates this <see cref="BxDF"/> from <see cref="outgoing"/> to <paramref name="incident"/>.
	/// </summary>
	/// <param name="outgoing">The unit local direction from which we enter.</param>
	/// <param name="incident">The unit local direction towards which we exit.</param>
	/// <returns>The <see cref="RGB128"/> value evaluated.</returns>
	/// <seealso cref="Sample"/>
	public abstract RGB128 Evaluate(Float3 outgoing, Float3 incident);

	/// <summary>
	/// Calculates the pdf of selecting <paramref name="incident"/> from <see cref="outgoing"/> with <see cref="Sample"/>.
	/// </summary>
	/// <param name="outgoing">The unit local source direction from which we enter.</param>
	/// <param name="incident">The selected unit local direction towards which we exit.</param>
	/// <returns>The probability density function (pdf) value of this selection.</returns>
	/// <seealso cref="Sample"/>
	public abstract float ProbabilityDensity(Float3 outgoing, Float3 incident);

	/// <summary>
	/// Samples <paramref name="incident"/> based on <paramref name="outgoing"/> for this <see cref="BxDF"/>.
	/// </summary>
	/// <param name="sample">The <see cref="Sample2D"/> used to sample <paramref name="incident"/>.</param>
	/// <param name="outgoing">The unit local source direction from which we enter.</param>
	/// <param name="incident">The sampled unit local direction towards which we exit.</param>
	/// <returns>The <see cref="Probable{T}"/> value evaluated from <paramref name="outgoing"/> to <paramref name="incident"/>.</returns>
	public abstract Probable<RGB128> Sample(Sample2D sample, Float3 outgoing, out Float3 incident);

	/// <summary>
	/// Returns the local surface normal that lies in the same hemisphere as <paramref name="direction"/>.
	/// </summary>
	public static Float3 Normal(Float3 direction) => CosineP(direction) < 0f ? Float3.Backward : Float3.Forward;

	/// <summary>
	/// Returns the cosine value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float CosineP(Float3 direction) => direction.Z;

	/// <summary>
	/// Returns the cosine squared value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float CosineP2(Float3 direction) => direction.Z * direction.Z;

	/// <summary>
	/// Returns the sine squared value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float SineP2(Float3 direction) => FastMath.OneMinus2(direction.Z);

	/// <summary>
	/// Returns the sine value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float SineP(Float3 direction) => FastMath.Identity(CosineP(direction));

	/// <summary>
	/// Returns the tangent value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float TangentP(Float3 direction) => SineP(direction) / CosineP(direction);

	/// <summary>
	/// Returns the tangent squared value of the vertical angle phi between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float TangentP2(Float3 direction) => SineP2(direction) / CosineP2(direction);

	/// <summary>
	/// Returns the cosine value of the horizontal angle theta between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float CosineT(Float3 direction)
	{
		float sin = SineP(direction);
		if (FastMath.AlmostZero(sin)) return 1f;
		return FastMath.Clamp11(direction.X / sin);
	}

	/// <summary>
	/// Returns the sine value of the horizontal angle theta between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float SineT(Float3 direction)
	{
		float sin = SineP(direction);
		if (FastMath.AlmostZero(sin)) return 0f;
		return FastMath.Clamp11(direction.Y / sin);
	}

	/// <summary>
	/// Returns the cosine squared value of the horizontal angle theta between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float CosineT2(Float3 direction)
	{
		float sin2 = SineP2(direction);
		if (FastMath.AlmostZero(sin2)) return 1f;
		return FastMath.Clamp01(direction.X * direction.X / sin2);
	}

	/// <summary>
	/// Returns the sine squared value of the horizontal angle theta between local <paramref name="direction"/> and the local normal.
	/// </summary>
	public static float SineT2(Float3 direction)
	{
		float sin2 = SineP2(direction);
		if (FastMath.AlmostZero(sin2)) return 0f;
		return FastMath.Clamp01(direction.Y * direction.Y / sin2);
	}

	/// <summary>
	/// Returns whether either of the local directions <paramref name="direction0"/> and <paramref name="direction1"/> are
	/// flat against the local normal (dot = 0 with <see cref="Float3.Forward"/>) or if they are in the same hemisphere.
	/// </summary>
	protected static bool FlatOrSameHemisphere(Float3 direction0, Float3 direction1) => !FastMath.Positive(-CosineP(direction0) * CosineP(direction1));

	/// <summary>
	/// Returns whether either of the local directions <paramref name="direction0"/> and <paramref name="direction1"/> are
	/// flat against the local normal (dot = 0 with <see cref="Float3.Forward"/>) or if they are in opposite hemispheres.
	/// </summary>
	protected static bool FlatOrOppositeHemisphere(Float3 direction0, Float3 direction1) => !FastMath.Positive(CosineP(direction0) * CosineP(direction1));
}