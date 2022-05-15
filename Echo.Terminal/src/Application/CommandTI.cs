using System;
using CodeHelpers.Packed;
using Echo.Terminal.Core.Display;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal.Application;

public class CommandTI : AreaTI
{
	protected override void Paint(in Canvas canvas)
	{
		var brush = new Brush();

		canvas.FillAll(ref brush);
	}
}