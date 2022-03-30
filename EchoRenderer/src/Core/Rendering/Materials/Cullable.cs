using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;

namespace EchoRenderer.Core.Rendering.Materials;

/// <summary>
/// A <see cref="Material"/> that makes the surfaces it was assigned to single-sided,
/// so on one side of the surface the <see cref="Base"/> material behaves as regular,
/// but on the opposite side the surface is perceived as completely transparent.
/// </summary>
public class Cullable : Material, IEmissive
{
	NotNull<Material> _base = Invisible.instance;

	/// <summary>
	/// The <see cref="Material"/> that is used on the surfaces that are not culled.
	/// </summary>
	public Material Base
	{
		get => _base;
		set => _base = value;
	}

	/// <summary>
	/// Whether this <see cref="Cullable"/> culls the backface or the front face.
	/// The front face is the side that positively aligns with the surface normal.
	/// </summary>
	public bool Backface { get; set; } = true;

	/// <inheritdoc/>
	public float Power => emissive?.Power / 2f ?? 0f;

	IEmissive emissive;

	public override void Prepare()
	{
		Assert.IsTrue(Base is not Cullable);

		base.Prepare();
		Base.Prepare();

		emissive = Base is IEmissive casted && FastMath.Positive(casted.Power) ? casted : null;
	}

	public override void Scatter(ref Touch touch, Allocator allocator)
	{
		if (!Cull(touch)) Base.Scatter(ref touch, allocator);
		else Invisible.instance.Scatter(ref touch, allocator);
	}

	/// <inheritdoc/>
	public Float3 Emit(in GeometryPoint point, in Float3 outgoing)
	{
		bool none = emissive == null || Cull(point.normal, outgoing);
		return none ? Float3.Zero : emissive.Emit(point, outgoing);
	}

	bool Cull(in Touch touch) => Cull(touch.point.normal, touch.outgoing);

	bool Cull(in Float3 normal, in Float3 outgoing) => FastMath.Positive(outgoing.Dot(normal)) != Backface;
}