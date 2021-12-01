using CodeHelpers.Diagnostics;
using EchoRenderer.Mathematics.Accelerators;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Memory;

namespace EchoRenderer.Rendering.Materials
{
	/// <summary>
	/// Represents a completely invisible material. <see cref="PressedPack"/> Will omit all geometry tagged with this material.
	/// </summary>
	public class Invisible : Material
	{
		public override void Scatter(ref Interaction interaction, Arena arena)
		{
			DebugHelper.LogWarning("Attempting to scatter an invisible material!");
			interaction.bsdf = null;
		}
	}
}