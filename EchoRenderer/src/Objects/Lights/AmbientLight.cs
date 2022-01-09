using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Objects.Preparation;
using EchoRenderer.Rendering.Distributions;
using EchoRenderer.Textures.Directional;

namespace EchoRenderer.Objects.Lights
{
	/// <summary>
	/// An infinitely large directional light that surrounds the entire scene.
	/// </summary>
	public class AmbientLight : LightSource
	{
		public AmbientLight() : base(LightType.area | LightType.infinite) { }

		NotNull<object> _texture = Textures.Texture.black; //Interfaces and implicit casts are not so nice to each other so object is used here

		public IDirectionalTexture Texture
		{
			get => (IDirectionalTexture)_texture.Value;
			set => _texture = new NotNull<object>(value);
		}

		public override void Prepare(PreparedScene scene)
		{
			base.Prepare(scene);
			Texture.Prepare();
		}

		/// <summary>
		/// Evaluates this <see cref="AmbientLight"/> for <paramref name="ray"/> that escaped the <see cref="PreparedScene"/> geometries.
		/// </summary>
		public Float3 Evaluate(in Ray ray) => Utilities.ToFloat3(Texture.Evaluate(ray.direction));

		public override Float3 Sample(in Interaction interaction, Distro2 distro, out Float3 incidentWorld, out float pdf, out float travel)
		{
			Vector128<float> value = Texture.Sample(distro, out incidentWorld, out pdf);
			travel = float.PositiveInfinity;
			return Utilities.ToFloat3(value);
		}

		public override float ProbabilityDensity(in Interaction interaction, in Float3 incidentWorld) => Texture.ProbabilityDensity(incidentWorld);
	}
}