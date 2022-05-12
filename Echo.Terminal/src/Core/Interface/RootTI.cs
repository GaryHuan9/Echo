using CodeHelpers.Packed;
using Echo.Terminal.Core.Display;

namespace Echo.Terminal.Core.Interface;

public class RootTI : BisectionTI
{
	Domain domain;

	public void DrawToConsole()
	{
		Draw(domain);
		domain.CopyToConsole();
	}

	protected override void Reorient()
	{
		base.Reorient();

		Int2 size = Max - Min;
		if (domain == default) domain = new Domain(size);
		if (domain.size != size) domain = domain.Resize(size);
	}
}