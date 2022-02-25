using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Core.Rendering.Distributions;

namespace EchoRenderer.Core.Rendering.Scattering;

/// <summary>
/// Perfectly uniform Lambertian diffuse reflection.
/// </summary>
public class LambertianReflection : BxDF
{
	public LambertianReflection() : base
	(
		FunctionType.reflective |
		FunctionType.diffuse
	) { }

	public void Reset(in Float3 newReflectance) => reflectance = newReflectance;

	Float3 reflectance;

	public override Float3 Evaluate(in Float3 outgoing, in Float3 incident) => reflectance * (1f / Scalars.PI);

	public override Float3 GetReflectance(in Float3             outgoing, ReadOnlySpan<Sample2D> samples)  => reflectance;
	public override Float3 GetReflectance(ReadOnlySpan<Sample2D> samples0, ReadOnlySpan<Sample2D> samples1) => reflectance;
}

/// <summary>
/// Perfectly uniform Lambertian diffuse transmission.
/// </summary>
public class LambertianTransmission : BxDF
{
	public LambertianTransmission() : base
	(
		FunctionType.transmissive |
		FunctionType.diffuse
	) { }

	public void Reset(in Float3 newTransmittance) => transmittance = newTransmittance;

	Float3 transmittance;

	public override Float3 Evaluate(in Float3 outgoing, in Float3 incident) => transmittance * (1f / Scalars.PI);

	public override Float3 GetReflectance(in Float3             outgoing, ReadOnlySpan<Sample2D> samples)  => transmittance;
	public override Float3 GetReflectance(ReadOnlySpan<Sample2D> samples0, ReadOnlySpan<Sample2D> samples1) => transmittance;
}