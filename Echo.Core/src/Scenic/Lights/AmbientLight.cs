using System;
using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Scenic.Hierarchies;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Directional;

namespace Echo.Core.Scenic.Lights;

/// <summary>
/// A directional <see cref="InfiniteLight"/> that surrounds the entirety of a <see cref="Scene"/>.
/// </summary>
[EchoSourceUsable]
public class AmbientLight : InfiniteLight
{
	NotNull<object> _texture = Pure.white;

	/// <summary>
	/// The <see cref="IDirectionalTexture"/> applied to this <see cref="AmbientLight"/>.
	/// </summary>
	/// <remarks>The defaults is <see cref="Pure.white"/>.</remarks>
	[EchoSourceUsable]
	public IDirectionalTexture Texture
	{
		get => (IDirectionalTexture)_texture.Value;
		set => _texture = new NotNull<object>(value);
	}

	Float3x3 localToWorld; //From local-space to world-space, rotation only
	Float3x3 worldToLocal; //From world-space to local-space, rotation only

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
		float radius = Math.Max(scene.accelerator.SphereBound.radius, 0.1f);
		float multiplier = Scalars.Pi * radius * radius;
		_power = multiplier * Texture.Average.Luminance;
	}

	public override RGB128 Evaluate(in Float3 direction) => Intensity * Texture.Evaluate(worldToLocal * direction);

	public override Probable<RGB128> Sample(in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel)
	{
		Probable<RGB128> value = Texture.Sample(sample, out incident);

		incident = localToWorld * incident;
		travel = float.PositiveInfinity;

		return (Intensity * value.content, value.pdf);
	}

	public override float ProbabilityDensity(in GeometryPoint origin, in Float3 incident)
	{
		Float3 transformed = worldToLocal * incident;
		return Texture.ProbabilityDensity(transformed);
	}
}