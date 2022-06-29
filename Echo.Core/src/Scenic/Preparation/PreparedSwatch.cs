using CodeHelpers.Diagnostics;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Instancing;

namespace Echo.Core.Scenic.Preparation;

/// <summary>
/// A <see cref="MaterialSwatch"/> that is prepared for fast interactions.
/// </summary>
public readonly struct PreparedSwatch
{
	public PreparedSwatch(Material[] materials)
	{
#if DEBUG
		foreach (Material material in materials) Assert.IsNotNull(material);
#endif
		this.materials = materials;
	}

	readonly Material[] materials;

	/// <summary>
	/// The number of materials stored in this swatch.
	/// </summary>
	public int Count => materials.Length;

	/// <summary>
	/// Index this <see cref="PreparedSwatch"/> at <paramref name="index"/>.
	/// </summary>
	public Material this[MaterialIndex index] => materials[index];
}