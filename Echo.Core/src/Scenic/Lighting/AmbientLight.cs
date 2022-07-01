using System;
using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Directional;

namespace Echo.Core.Scenic.Lighting;

/// <summary>
/// An infinitely large directional light that surrounds the entire scene.
/// </summary>
public class AmbientLight : InfiniteLight
{
	NotNull<object> _texture = Textures.Texture.black; //Interfaces and implicit casts are not so nice to each other so object is used here

	public IDirectionalTexture Texture
	{
		get => (IDirectionalTexture)_texture.Value;
		set => _texture = new NotNull<object>(value);
	}

	Float3x3 localToWorld; //From local-space to world-space, rotation only
	Float3x3 worldToLocal; //From world-space to local-space, rotation only

	public override Float3 Position
	{
		set
		{
			if (value.EqualsExact(Position)) return;
			ThrowModifyTransformException();
		}
	}

	public override float Scale
	{
		set
		{
			if (value.Equals(Scale)) return;
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
		localToWorld = ContainedRotation;
		worldToLocal = localToWorld.Inverse;

		//Calculate power
		float radius = scene.accelerator.SphereBounds.radius;
		float multiplier = Scalars.Pi * radius * radius;
		_power = multiplier * Texture.Average.Luminance;
	}

	/// <summary>
	/// Evaluates this <see cref="AmbientLight"/> at <paramref name="direction"/>
	/// in world-space, which escaped the <see cref="PreparedSceneOld"/> geometries.
	/// </summary>
	public RGB128 Evaluate(in Float3 direction) => Texture.Evaluate(worldToLocal * direction);

	public override Probable<RGB128> Sample(in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel)
	{
		Probable<RGB128> value = Texture.Sample(sample, out incident);

		incident = localToWorld * incident;
		travel = float.PositiveInfinity;

		return value;
	}

	public override float ProbabilityDensity(in GeometryPoint origin, in Float3 incident)
	{
		Float3 transformed = worldToLocal * incident;
		return Texture.ProbabilityDensity(transformed);
	}

	static void ThrowModifyTransformException() => throw new Exception($"Cannot modify {nameof(AmbientLight)} transform!");
}