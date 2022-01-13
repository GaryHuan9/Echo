using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Objects.Preparation;
using EchoRenderer.Rendering.Distributions;

namespace EchoRenderer.Objects.Lights
{
	/// <summary>
	/// A light source that can create radiance and illuminate the scene.
	/// NOTE: unless explicitly indicated, directions in <see cref="LightSource"/> are world space.
	/// </summary>
	public abstract class LightSource : Object
	{
		protected LightSource(LightType type) => this.type = type;

		public readonly LightType type;

		/// <summary>
		/// The main color and intensity of this <see cref="LightSource"/>.
		/// </summary>
		public Float3 Intensity { get; set; } = Float3.one;

		/// <summary>
		/// The number of consecutive samples that should be used to sample this <see cref="LightSource"/>.
		/// </summary>
		public int SampleCount { get; set; } = 1;

		/// <summary>
		/// The approximated total emitted power of this <see cref="LightSource"/>.
		/// </summary>
		public virtual Float3 Power => throw new NotSupportedException();

		/// <summary>
		/// Invoked before rendering; after geometry and materials are prepared.
		/// Can be used to initialize this light to prepare it for rendering.
		/// </summary>
		public virtual void Prepare(PreparedScene scene) => Intensity = Intensity.Max(Float3.zero);

		/// <summary>
		/// Samples the contribution of this <see cref="LightSource"/> to <paramref name="interaction"/>.
		/// </summary>
		public abstract Float3 Sample(in Interaction interaction, Distro2 distro, out Float3 incident, out float pdf, out float travel);

		/// <summary>
		/// Returns the sampled probability density (pdf) for <paramref name="incident"/>
		/// to occur at <paramref name="interaction"/> for this <see cref="LightSource"/>.
		/// </summary>
		public abstract float ProbabilityDensity(in Interaction interaction, in Float3 incident);
	}
}