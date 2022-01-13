using System;
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

		Float3x3 localToWorld; //From local space to world space, rotation only
		Float3x3 worldToLocal; //From world space to local space, rotation only

		public override Float3 Position
		{
			set
			{
				if (value.EqualsExact(Position)) return;
				ThrowModifyTransformException();
			}
		}

		public override Float3 Scale
		{
			set
			{
				if (value.EqualsExact(Scale)) return;
				ThrowModifyTransformException();
			}
		}

		public override void Prepare(PreparedScene scene)
		{
			base.Prepare(scene);
			Texture.Prepare();

			localToWorld = new Versor(Rotation);
			worldToLocal = localToWorld.Inverse;
		}

		/// <summary>
		/// Evaluates this <see cref="AmbientLight"/> at <paramref name="direction"/> that escaped the <see cref="PreparedScene"/> geometries.
		/// </summary>
		public Float3 Evaluate(in Float3 direction)
		{
			Float3 transformed = worldToLocal * direction;
			return Utilities.ToFloat3(Texture.Evaluate(transformed));
		}

		public override Float3 Sample(in Interaction interaction, Distro2 distro, out Float3 incident, out float pdf, out float travel)
		{
			Vector128<float> value = Texture.Sample(distro, out incident, out pdf);

			incident = localToWorld * incident;
			travel = float.PositiveInfinity;
			return Utilities.ToFloat3(value);
		}

		public override float ProbabilityDensity(in Interaction interaction, in Float3 incident)
		{
			Float3 transformed = worldToLocal * incident;
			return Texture.ProbabilityDensity(transformed);
		}

		static void ThrowModifyTransformException() => throw new Exception($"Cannot modify {nameof(AmbientLight)} transform!");
	}
}