using System;
using CodeHelpers.Packed;
using Echo.Terminal.Core.Display;

namespace Echo.Terminal.Core.Interface;

public abstract class AreaTI //TI = Terminal Interface
{
	public Int2 Min { get; private set; }
	public Int2 Max { get; private set; }

	public bool InvertY { get; set; }

	public virtual void Draw(in Domain domain)
	{
		if (!(Max > Min)) return;
		Paint(domain.MakePainter(Min, Max, InvertY));
	}

	public void SetTransform(Int2 min, Int2 max)
	{
		if (Min == min && Max == max) return;

		if (min <= max)
		{
			Min = min;
			Max = max;
			Reorient();
		}
		else throw new ArgumentException("Invalid transform", nameof(min));
	}

	protected abstract void Paint(in Painter painter);

	protected virtual void Reorient() { }
}