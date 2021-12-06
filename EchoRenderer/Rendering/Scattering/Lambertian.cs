using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Rendering.Sampling;

namespace EchoRenderer.Rendering.Scattering
{
	/// <summary>
	/// Perfectly uniform Lambertian diffuse reflection.
	/// </summary>
	public class LambertianReflection : BxDF
	{
		public LambertianReflection() : base
		(
			FunctionType.reflection |
			FunctionType.diffuse
		) { }

		public void Reset(in Float3 newReflectance) => reflectance = newReflectance;

		Float3 reflectance;

		public override Float3 Sample(in Float3 outgoing, in Float3 incident) => reflectance * (1f / Scalars.PI);

		public override Float3 GetReflectance(in Float3             outgoing, ReadOnlySpan<Distro2> distros)  => reflectance;
		public override Float3 GetReflectance(ReadOnlySpan<Distro2> distros0, ReadOnlySpan<Distro2> distros1) => reflectance;
	}

	/// <summary>
	/// Perfectly uniform Lambertian diffuse transmission.
	/// </summary>
	public class LambertianTransmission : BxDF
	{
		public LambertianTransmission() : base
		(
			FunctionType.transmission |
			FunctionType.diffuse
		) { }

		public void Reset(in Float3 newTransmittance) => transmittance = newTransmittance;

		Float3 transmittance;

		public override Float3 Sample(in Float3 outgoing, in Float3 incident) => transmittance * (1f / Scalars.PI);

		public override Float3 GetReflectance(in Float3             outgoing, ReadOnlySpan<Distro2> distros)  => transmittance;
		public override Float3 GetReflectance(ReadOnlySpan<Distro2> distros0, ReadOnlySpan<Distro2> distros1) => transmittance;
	}
}