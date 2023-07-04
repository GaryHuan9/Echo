using Echo.Core.Aggregation.Preparation;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Scenic.Hierarchies;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lights;

/// <summary>
/// An <see cref="LightEntity"/> that is infinitely far away from the <see cref="Scene"/>.
/// </summary>
/// <remarks>All <see cref="InfiniteLight"/> must have an area;
/// delta <see cref="InfiniteLight"/> is not supported.</remarks>
public abstract class InfiniteLight : LightEntity
{
	public override Float3 Position
	{
		set => throw ModifyTransformException();
	}

	public override float Scale
	{
		set => throw ModifyTransformException();
	}

	/// <summary>
	/// Whether this <see cref="InfiniteLight"/> should be evaluated for immediately escaped light paths.
	/// </summary>
	[EchoSourceUsable]
	public bool DirectlyVisible { get; set; } = true;

	/// <summary>
	/// The total power of this <see cref="InfiniteLight"/>.
	/// </summary>
	/// <remarks>Should be initialized after <see cref="Prepare"/> is invoked.</remarks>
	public abstract float Power { get; }

	/// <summary>
	/// Whether this <see cref="InfiniteLight"/> is a Dirac delta distribution.
	/// </summary>
	public abstract bool IsDelta { get; }

	protected Float3x3 LocalToWorldRotation { get; private set; }
	protected Float3x3 WorldToLocalRotation { get; private set; }

	/// <summary>
	/// Invoked before rendering; after geometry and other lights are prepared.
	/// Can be used to initialize this infinite light to prepare it for rendering.
	/// </summary>
	public virtual void Prepare(PreparedScene scene)
	{
		LocalToWorldRotation = (Float3x3)ContainedRotation;
		WorldToLocalRotation = (Float3x3)ContainedRotation.Inverse;
	}

	/// <summary>
	/// Evaluates this <see cref="InfiniteLight"/>.
	/// </summary>
	/// <param name="incident">The normalized world-space direction to evaluate at.</param>
	public abstract RGB128 Evaluate(Float3 incident);

	/// <inheritdoc cref="IPreparedLight.ProbabilityDensity"/>
	public abstract float ProbabilityDensity(in GeometryPoint origin, Float3 incident);

	/// <inheritdoc cref="IPreparedLight.Sample"/>
	public abstract Probable<RGB128> Sample(in GeometryPoint origin, Sample2D sample, out Float3 incident, out float travel);

	protected override void CheckRoot(EntityPack root)
	{
		base.CheckRoot(root);
		if (root is Scene) return;

		throw new SceneException($"Cannot add an {nameof(InfiniteLight)} to an {nameof(EntityPack)} that is not a {nameof(Scene)}.");
	}

	static SceneException ModifyTransformException() => new($"Cannot modify the {nameof(Position)} nor the {nameof(Scale)} of an {nameof(InfiniteLight)}.");
}