namespace Echo.Core.Scenic.Hierarchies;

public class PackInstance : Entity
{
	EntityPack _pack;

	public EntityPack Pack
	{
		get => _pack;
		set
		{
			if (_pack == value) return;

			if (Root != null)
			{
				if (_pack != null) Root.RemoveInstance(_pack);

				if (Root.AllInstances.Contains(value)) throw null;

				if (value != null) Root.AddInstance(value);
			}

			_pack = value;
		}
	}

	public MaterialSwatch Swatch { get; set; }

	protected override bool CanAddRoot(EntityPack root) => Pack?.AllInstances.Contains(root) != true;
}