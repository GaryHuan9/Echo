using Echo.Terminal.Core.Display;

namespace Echo.Terminal.Core.Interface;

public sealed class FilledTI : AreaTI
{
	public char Filling { get; set; } = ' ';

	protected override void Paint(in Painter painter) => painter.FillAll(Filling);
}