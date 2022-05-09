using CodeHelpers.Diagnostics;

namespace Echo.Terminal.Core;

public abstract class AreaUI
{
	Domain _domain;

	public Domain Domain
	{
		get => _domain;
		set
		{
			if (_domain == value) return;

			var old = _domain;
			_domain = value;
			OnResize(old);
		}
	}

	public bool InvertY { get; set; }

	public virtual void Update()
	{
		if (Domain.size.MinComponent < 1) return;
		Draw(Domain.MakeDrawer(InvertY));
	}

	protected abstract void Draw(in Domain.Drawer drawer);

	protected virtual void OnResize(Domain previous) => Assert.AreNotEqual(previous, Domain);
}