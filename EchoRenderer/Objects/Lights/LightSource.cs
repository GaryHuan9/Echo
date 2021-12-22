using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Rendering;
using EchoRenderer.Rendering.Distributions;

namespace EchoRenderer.Objects.Lights
{
	/// <summary>
	/// A directional light. Infinitely far away pointed towards the local positive z axis.
	/// </summary>
	public abstract class LightSource : Object
	{
		/// <summary>
		/// The main color and intensity of this <see cref="LightSource"/>.
		/// </summary>
		public Float3 Intensity { get; set; } = Float3.one;

		/// <summary>
		/// The number of consecutive samples that should be used to sample this <see cref="LightSource"/>.
		/// </summary>
		public int SampleCount { get; set; } = 1;

		/// <summary>
		/// Returns whether this <see cref="LightSource"/> is a delta <see cref="LightSource"/>,
		/// which means it only starts at a point or only exists in one direction.
		/// </summary>
		public abstract bool IsDelta { get; }

		/// <summary>
		/// Invoked before rendering; after geometry and materials are pressed.
		/// Can be used to initialize this light to prepare it for rendering.
		/// </summary>
		public virtual void Press(PressedScene scene) => Intensity = Intensity.Max(Float3.zero);

		/// <summary>
		/// Samples the contribution of this <see cref="LightSource"/> to <paramref name="interaction"/>.
		/// </summary>
		public abstract Float3 Sample(in Interaction interaction, in Distro2 distro, out Float3 incidentWorld, out float pdf, out float travel);

		/// <summary>
		/// Returns the sampled probability density (pdf) for a given pair of
		/// <paramref name="interaction"/> and <paramref name="incident"/> direction.
		/// </summary>
		public abstract float ProbabilityDensity(in Interaction interaction, in Float3 incident);
	}
}