using System;
using CodeHelpers;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Directional;

namespace Echo.Core.Scenic.Lights;

/// <summary>
/// A directional <see cref="InfiniteLight"/> that surrounds the entirety of a <see cref="Scene"/>.
/// </summary>
public class AmbientLight : InfiniteLight
{
	NotNull<object> _texture = Pure.black;

	/// <summary>
	/// The <see cref="IDirectionalTexture"/> applied to this <see cref="AmbientLight"/>.
	/// Defaults to <see cref="Pure.black"/>.
	/// </summary>
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
		float radius = scene.accelerator.SphereBound.radius;
		float multiplier = Scalars.Pi * radius * radius;
		_power = multiplier * Texture.Average.Luminance;
	}

	public override RGB128 Evaluate(in Float3 direction) => Texture.Evaluate(worldToLocal * direction);

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