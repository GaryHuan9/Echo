using CodeHelpers;
using Echo.Core.Evaluation.Materials;
using Echo.Core.Scenic.Hierarchies;

namespace Echo.Core.Scenic.Geometries;

public abstract class MaterialEntity : Entity
{
	NotNull<Material> _material = Invisible.instance;

	/// <summary>
	/// The <see cref="Echo.Core.Evaluation.Materials.Material"/> applied to this <see cref="MaterialEntity"/>.
	/// </summary>
	/// <remarks>This value must not be null.</remarks>
	public Material Material
	{
		get => _material;
		set => _material = value;
	}
}