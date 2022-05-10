using Echo.Terminal.Core.Display;

namespace Echo.Terminal.Core.Interface;

public sealed class FilledTI : AreaTI
{
	public char Filling { get; set; } = ' ';

	protected override void Draw(in Domain.Drawer drawer) => drawer.FillAll(Filling);
}