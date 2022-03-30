using CodeHelpers.Packed;

namespace EchoRenderer.UserInterface.Core.Areas;

public class AutoLayoutAreaUI : AreaUI
{
	public bool Horizontal { get; set; }

	public bool Margins { get; set; } = true;
	public bool Spaces { get; set; } = true;

	//Additional margin sizes used on minor axes
	public float PositiveMargin { get; set; }
	public float NegativeMargin { get; set; }

	protected override void Reorient(Float2 position, Float2 dimension)
	{
		base.Reorient(position, dimension);
		int count = ChildCount;

		if (count == 0)
		{
			transform.PreferedHeight = null;
			return;
		}

		float marginSize = Margins ? Theme.MediumMargin : 0f;
		float spaceSize = Spaces ? Theme.MediumMargin : 0f;

		if (Horizontal)
		{
			float margin = marginSize / Dimension.X;
			float space = spaceSize / Dimension.X;

			float total = 1f - margin * 2f - (count - 1) * space;

			float width = total / count;
			float current = margin;

			foreach (AreaUI child in LoopForward())
			{
				Transform target = child.transform;

				target.LeftPercent = current;
				current += width;
				target.RightPercent = 1f - current;

				target.HorizontalMargins = 0f;
				target.VerticalPercents = 0f;

				target.TopMargin = marginSize + PositiveMargin;
				target.BottomMargin = marginSize + NegativeMargin;

				current += space;
			}
		}
		else
		{
			float current = marginSize;

			foreach (AreaUI child in LoopForward())
			{
				Transform target = child.transform;
				float height = target.PreferedHeight ?? Theme.LayoutHeight;

				//Vertical layouts are controlled solely by margins
				target.TopMargin = current;
				current += height;
				target.BottomMargin = -current;

				target.TopPercent = 0f;
				target.BottomPercent = 1f;
				target.HorizontalPercents = 0f;

				target.RightMargin = marginSize + PositiveMargin;
				target.LeftMargin = marginSize + NegativeMargin;

				current += spaceSize;
			}

			transform.PreferedHeight = current + marginSize - spaceSize;
		}
	}
}