﻿using Echo.Terminal.Core.Display;

namespace Echo.Terminal.Core.Interface;

public class BisectionTI : ParentTI
{
	public BisectionTI() => DividerSize = 1;

	protected override void Paint(in Canvas canvas) => canvas.FillAll(Horizontal ? '\u2502' : '\u2500');
}