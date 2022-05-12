using Echo.Terminal.Core.Display;

namespace Echo.Terminal.Core.Interface;

public class BisectionTI : ParentTI
{
	public BisectionTI() => DividerSize = 1;

	protected override void Paint(in Painter painter)
	{
		if (Horizontal)
		{
			int height = painter.size.Y;
			for (int y = 0; y < height; y++) painter[0, y] = '\u2502';
		}
		else painter.FillLine(0, '\u2500');

		//TODO: connect vertical and horizontal divisors
	}
}