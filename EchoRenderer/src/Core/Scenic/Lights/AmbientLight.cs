using System;
using System.Runtime.Intrinsics;
using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Scenic.Preparation;
using EchoRenderer.Core.Texturing.Directional;

namespace EchoRenderer.Core.Scenic.Lights;

/// <summary>
/// An infinitely large directional light that surrounds the entire scene.
/// </summary>
public class AmbientLight : AreaLightSource
{
	NotNull<object> _texture = Texturing.Texture.black; //Interfaces and implicit casts are not so nice to each other so object is used here

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

	float _power;

	public override float Power => _power;

	public override void Prepare(PreparedScene scene)
	{
		base.Prepare(scene);
		Texture.Prepare();

		//Calculate transforms
		localToWorld = new Versor(Rotation);
		worldToLocal = localToWorld.Inverse;

		//Calculate power
		float radius = scene.info.boundingSphere.radius;
		float multiplier = Scalars.Pi * radius * radius;
		_power = multiplier * Texture.Average.Luminance;
	}

	/// <summary>
	/// Evaluates this <see cref="AmbientLight"/> at <paramref name="direction"/>
	/// in world-space, which escaped the <see cref="PreparedScene"/> geometries.
	/// </summary>
	public RGBA32 Evaluate(in Float3 direction) => Texture.Evaluate(worldToLocal * direction);

	public override Probable<RGBA32> Sample(in GeometryPoint point, Sample2D sample, out Float3 incident, out float travel)
	{
		Probable<RGBA32> value = Texture.Sample(sample, out incident);

		incident = localToWorld * incident;
		travel = float.PositiveInfinity;

		return value;
	}

	public override float ProbabilityDensity(in GeometryPoint point, in Float3 incident)
	{
		Float3 transformed = worldToLocal * incident;
		return Texture.ProbabilityDensity(transformed);
	}

	static void ThrowModifyTransformException() => throw new Exception($"Cannot modify {nameof(AmbientLight)} transform!");
}