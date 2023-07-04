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
public sealed class AmbientLight : InfiniteLight
{
	NotNull<IDirectionalTexture> _texture = Pure.white;

	/// <summary>
	/// The <see cref="IDirectionalTexture"/> applied to this <see cref="AmbientLight"/>.
	/// </summary>
	/// <remarks>The defaults is <see cref="Pure.white"/>.</remarks>
	[EchoSourceUsable]
	public IDirectionalTexture Texture
	{
		get => _texture.Value;
		set => _texture = new NotNull<IDirectionalTexture>(value);
	}

	float _power;

	public override float Power => _power;
	public override bool IsDelta => false;

	public override void Prepare(PreparedScene scene)
	{
		base.Prepare(scene);
		Texture.Prepare();

		//Calculate power
		float radius = Math.Max(scene.accelerator.SphereBound.radius, 1f);
		float luminance = Texture.Average.Luminance * Intensity.Luminance;
		_power = Scalars.Pi * radius * radius * luminance;
	}

	public override RGB128 Evaluate(Float3 incident) =>
		Intensity * Texture.Evaluate(WorldToLocalRotation * incident);

	public override float ProbabilityDensity(in GeometryPoint origin, Float3 incident) =>
		Texture.ProbabilityDensity(WorldToLocalRotation * incident);

	public override Probable<RGB128> Sample(in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel)
	{
		(RGB128 sampled, float pdf) = Texture.Sample(sample, out incident);
		incident = LocalToWorldRotation * incident;

		travel = float.PositiveInfinity;
		return (sampled * Intensity, pdf);
	}
}