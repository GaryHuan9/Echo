using CodeHelpers;
using Echo.Core.Evaluation.Materials;

namespace Echo.Core.Scenic.Geometric;

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