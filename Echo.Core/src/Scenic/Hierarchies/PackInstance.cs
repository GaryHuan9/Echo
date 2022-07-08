using Echo.Core.Evaluation.Materials;

namespace Echo.Core.Scenic.Hierarchies;

/// <summary>
/// An <see cref="Entity"/> that allows for the instancing of an <see cref="EntityPack"/>.
/// </summary>
public class PackInstance : Entity
{
	EntityPack _pack;

	/// <summary>
	/// The <see cref="EntityPack"/> to instance.
	/// </summary>
	/// <exception cref="SceneException">Thrown if an <see cref="EntityPack"/> is found to be recursively instanced.</exception>
	public EntityPack Pack
	{
		get => _pack;
		set
		{
			if (_pack == value) return;
			if (value is Scene) throw new SceneException($"Attempting to instance a {nameof(Scene)}.");

			if (Root != null)
			{
				if (_pack != null) Root.RemoveInstance(_pack);

				if (value != null)
				{
					if (!value.AllInstances.Contains(Root)) Root.AddInstance(value);
					else throw RecursivelyInstancedEntityPackException();
				}
			}

			_pack = value;
		}
	}

	/// <summary>
	/// A custom <see cref="MaterialSwatch"/> used to optionally modify the <see cref="Material"/>s applied the instanced <see cref="EntityPack"/>.
	/// </summary>
	public MaterialSwatch Swatch { get; set; }

	protected override void CheckRoot(EntityPack root)
	{
		base.CheckRoot(root);

		if (Pack?.AllInstances.Contains(root) != true) return;
		throw RecursivelyInstancedEntityPackException();
	}

	static SceneException RecursivelyInstancedEntityPackException() => new($"Found an {nameof(EntityPack)} being recursively instanced.");
}