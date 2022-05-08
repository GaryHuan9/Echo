using CodeHelpers.Diagnostics;

namespace Echo.Terminal.Areas;

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

	public abstract void Update();

	protected virtual void OnResize(Domain previous)
	{
		Assert.AreNotEqual(previous, Domain);
	}
}