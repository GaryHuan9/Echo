using System;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

/// <summary>
/// A <see cref="Material"/> that makes the surfaces it was assigned to single-sided,
/// so on one side of the surface the <see cref="Base"/> material behaves as regular,
/// but on the opposite side the surface is perceived as completely transparent.
/// </summary>
public sealed class OneSided : Material
{
	NotNull<Material> _base = Invisible.instance;

	/// <summary>
	/// The <see cref="Material"/> that is used on the surfaces that are not culled.
	/// </summary>
	/// <remarks>This <see cref="Material"/> cannot be of type <see cref="Emissive"/>.</remarks>
	public Material Base
	{
		get => _base;
		set
		{
			if (value is not Emissive) _base = value;
			else throw new ArgumentOutOfRangeException(nameof(value));
		}
	}

	/// <summary>
	/// Whether this <see cref="OneSided"/> culls the backface or the front face.
	/// The front face is the side that positively aligns with the surface normal.
	/// </summary>
	public bool Backface { get; set; } = true;

	public override void Prepare()
	{
		base.Prepare();
		Base.Prepare();
	}

	public override void Scatter(ref Contact contact, Allocator allocator)
	{
		if (!Cull(contact)) Base.Scatter(ref contact, allocator);
		else Invisible.instance.Scatter(ref contact, allocator);
	}

	public override BSDF Scatter(in Contact contact, Allocator allocator, in RGB128 albedo) => throw new NotSupportedException();

	bool Cull(in Contact contact) => FastMath.Positive(contact.outgoing.Dot(contact.point.normal)) != Backface;
}