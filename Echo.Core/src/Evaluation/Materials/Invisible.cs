using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;

namespace Echo.Core.Evaluation.Materials;

/// <summary>
/// Represents a completely invisible <see cref="Material"/>, which
/// generate no <see cref="BSDF"/> for <see cref="Contact.bsdf"/>
/// </summary>
public sealed class Invisible : Material
{
	/// <summary>
	/// A static instance of this <see cref="Invisible"/> that can be reused.
	/// </summary>
	public static readonly Invisible instance = new();

	public override void Scatter(ref Contact contact, Allocator allocator) => contact.bsdf = null;
}