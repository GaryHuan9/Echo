using System;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Scattering;

/// <summary>
/// Perfectly uniform Lambertian diffuse reflection.
/// </summary>
public class LambertianReflection : BxDF
{
	public LambertianReflection() : base
	(
		FunctionType.Reflective |
		FunctionType.Diffuse
	) { }

	public void Reset(in RGB128 newReflectance) => reflectance = newReflectance;

	RGB128 reflectance;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => reflectance * Scalars.PiR;

	public override RGB128 GetReflectance(in Float3 outgoing, ReadOnlySpan<Sample2D> samples) => reflectance;
	public override RGB128 GetReflectance(ReadOnlySpan<Sample2D> samples0, ReadOnlySpan<Sample2D> samples1) => reflectance;
}

/// <summary>
/// Perfectly uniform Lambertian diffuse transmission.
/// </summary>
public class LambertianTransmission : BxDF
{
	public LambertianTransmission() : base
	(
		FunctionType.Transmissive |
		FunctionType.Diffuse
	) { }

	public void Reset(in RGB128 newTransmittance) => transmittance = newTransmittance;

	RGB128 transmittance;

	public override RGB128 Evaluate(in Float3 outgoing, in Float3 incident) => transmittance * Scalars.PiR;

	public override RGB128 GetReflectance(in Float3 outgoing, ReadOnlySpan<Sample2D> samples) => transmittance;
	public override RGB128 GetReflectance(ReadOnlySpan<Sample2D> samples0, ReadOnlySpan<Sample2D> samples1) => transmittance;
}