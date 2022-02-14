using CodeHelpers.Diagnostics;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Rendering.Materials;

/// <summary>
/// Represents a completely invisible material. <see cref="PreparedPack"/> Will omit all geometry tagged with this material.
/// </summary>
public class Invisible : Material
{
	public override void Scatter(ref Interaction interaction, Arena arena)
	{
		DebugHelper.LogWarning("Attempting to scatter an invisible material!");
		interaction.bsdf = null;
	}
}