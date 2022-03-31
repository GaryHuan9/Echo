using System;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics.Primitives;
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

	public void Reset(in RGBA128 newReflectance) => reflectance = newReflectance;

	RGBA128 reflectance;

	public override RGBA128 Evaluate(in Float3 outgoing, in Float3 incident) => reflectance * Scalars.PiR;

	public override RGBA128 GetReflectance(in Float3 outgoing, ReadOnlySpan<Sample2D> samples) => reflectance;
	public override RGBA128 GetReflectance(ReadOnlySpan<Sample2D> samples0, ReadOnlySpan<Sample2D> samples1) => reflectance;
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

	public void Reset(in RGBA128 newTransmittance) => transmittance = newTransmittance;

	RGBA128 transmittance;

	public override RGBA128 Evaluate(in Float3 outgoing, in Float3 incident) => transmittance * Scalars.PiR;

	public override RGBA128 GetReflectance(in Float3 outgoing, ReadOnlySpan<Sample2D> samples) => transmittance;
	public override RGBA128 GetReflectance(ReadOnlySpan<Sample2D> samples0, ReadOnlySpan<Sample2D> samples1) => transmittance;
}