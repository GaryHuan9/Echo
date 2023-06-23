using System;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

/// <summary>
/// Represents a completely invisible <see cref="Material"/>, which
/// generate no <see cref="BSDF"/> for <see cref="Contact.bsdf"/>
/// </summary>
[EchoSourceUsable]
public sealed class Invisible : Material
{
	/// <summary>
	/// A static instance of this <see cref="Invisible"/> that can be reused.
	/// </summary>
	public static readonly Invisible instance = new();

	public override void Scatter(ref Contact contact, Allocator allocator)
	{
		contact.bsdf = NewBSDF(contact, allocator, RGB128.White);
		contact.bsdf.Add<SpecularTransmission>(allocator).Reset(new RealFresnel(1f, 1f));
	}

	protected override BSDF Scatter(in Contact contact, Allocator allocator, in RGB128 albedo) => throw new NotSupportedException();
}