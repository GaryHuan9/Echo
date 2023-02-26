using Echo.Core.Common.Diagnostics;
using Echo.Core.Evaluation.Materials;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Scenic.Hierarchies;

namespace Echo.Core.Scenic.Geometries;

/// <summary>
/// An <see cref="Entity"/> that allows a <see cref="Material"/> to be applied to it; usually a geometric <see cref="Entity"/>.
/// </summary>
public abstract class MaterialEntity : Entity
{
	NotNull<Material> _material = Invisible.instance;

	/// <summary>
	/// The <see cref="Echo.Core.Evaluation.Materials.Material"/> applied to this <see cref="MaterialEntity"/>.
	/// </summary>
	/// <remarks>This value must not be null.</remarks>
	[EchoSourceUsable]
	public Material Material
	{
		get => _material;
		set => _material = value;
	}
}