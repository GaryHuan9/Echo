using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Scattering;

namespace EchoRenderer.Core.Rendering.Materials;

/// <summary>
/// Represents a completely invisible <see cref="Material"/>, which
/// generate no <see cref="BSDF"/> for <see cref="Touch.bsdf"/>
/// </summary>
public class Invisible : Material
{
	/// <summary>
	/// A static instance of this <see cref="Invisible"/> that can be reused.
	/// </summary>
	public static readonly Invisible instance = new();

	public override void Scatter(ref Touch touch, Arena arena) => touch.bsdf = null;
}