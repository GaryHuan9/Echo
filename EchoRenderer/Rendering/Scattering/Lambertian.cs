using System;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Rendering.Scattering
{
	/// <summary>
	/// Perfectly uniform Lambertian diffuse reflection.
	/// </summary>
	public class LambertianReflection : BidirectionalDistributionFunction
	{
		public LambertianReflection() : base
		(
			BidirectionalDistributionFunctionType.reflection |
			BidirectionalDistributionFunctionType.diffuse
		) { }

		public void Reset(in Float3 newReflectance) => reflectance = newReflectance;

		Float3 reflectance;

		public override Float3 Sample(in Float3 outgoing, in Float3 incident) => reflectance * (1f / Scalars.PI);

		public override Float3 GetReflectance(in Float3            outLocal, ReadOnlySpan<Float2> samples)  => reflectance;
		public override Float3 GetReflectance(ReadOnlySpan<Float2> samples0, ReadOnlySpan<Float2> samples1) => reflectance;
	}

	/// <summary>
	/// Perfectly uniform Lambertian diffuse transmission.
	/// </summary>
	public class LambertianTransmission : BidirectionalDistributionFunction
	{
		public LambertianTransmission() : base
		(
			BidirectionalDistributionFunctionType.transmission |
			BidirectionalDistributionFunctionType.diffuse
		) { }

		public void Reset(in Float3 newTransmittance) => transmittance = newTransmittance;

		Float3 transmittance;

		public override Float3 Sample(in Float3 outgoing, in Float3 incident) => transmittance * (1f / Scalars.PI);

		public override Float3 GetReflectance(in Float3            outLocal, ReadOnlySpan<Float2> samples)  => transmittance;
		public override Float3 GetReflectance(ReadOnlySpan<Float2> samples0, ReadOnlySpan<Float2> samples1) => transmittance;
	}
}