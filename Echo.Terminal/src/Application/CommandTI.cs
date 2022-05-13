using Echo.Terminal.Core.Display;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal.Application;

public class CommandTI : AreaTI
{
	protected override void Paint(in Painter painter)
	{
		painter.FillAll();
	}
}