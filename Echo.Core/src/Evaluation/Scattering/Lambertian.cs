using System;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

/// <summary>
/// Perfectly uniform Lambertian diffuse reflection.
/// </summary>
public sealed class LambertianReflection : BxDF
{
	public LambertianReflection() : base
	(
		FunctionType.Reflective |
		FunctionType.Diffuse
	) { }

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => new(Scalars.PiR);

	public override RGB128 GetReflectance(in Float3 outgoing, ReadOnlySpan<Sample2D> samples) => RGB128.White;
	public override RGB128 GetReflectance(ReadOnlySpan<Sample2D> samples0, ReadOnlySpan<Sample2D> samples1) => RGB128.White;
}

/// <summary>
/// Perfectly uniform Lambertian diffuse transmission.
/// </summary>
public sealed class LambertianTransmission : BxDF
{
	public LambertianTransmission() : base
	(
		FunctionType.Transmissive |
		FunctionType.Diffuse
	) { }

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => new(Scalars.PiR);

	public override RGB128 GetReflectance(in Float3 outgoing, ReadOnlySpan<Sample2D> samples) => RGB128.White;
	public override RGB128 GetReflectance(ReadOnlySpan<Sample2D> samples0, ReadOnlySpan<Sample2D> samples1) => RGB128.White;
}