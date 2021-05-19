using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;

namespace EchoRenderer.UI.Core.Areas
{
	public class AutoLayoutAreaUI : AreaUI
	{
		public bool Horizontal { get; set; }

		public bool Margins { get; set; } = true;
		public bool Gaps { get; set; } = true;

		public override void Update()
		{
			base.Update();

			int count = ChildCount;
			if (count == 0) return;

			float marginSize = Margins ? Theme.LargeMargin : 0f;
			float gapSize = Gaps ? Theme.LargeMargin : 0f;

			if (Horizontal)
			{
				float gap = gapSize / Size.x;
				float margin = marginSize / Size.x;

				float total = 1f - margin * 2f - (count - 1) * gap;

				float width = total / count;
				float current = margin;

				foreach (AreaUI child in this)
				{
					child.transform.LeftPercent = current;
					current += width;

					child.transform.RightPercent = 1f - current;
					child.transform.HorizontalMargins = 0f;

					child.transform.VerticalMargins = marginSize;
					child.transform.VerticalPercents = 0f;

					current += gap;
				}
			}
			else
			{
				float total = Size.y;
				float current = marginSize;

				foreach (AreaUI child in this)
				{
					//Vertical layouts are solely controlled by margins
					child.transform.TopMargin = current;
					current += Theme.LayoutHeight;

					child.transform.BottomMargin = total - current;
					child.transform.VerticalPercents = 0f;

					child.transform.HorizontalMargins = marginSize;
					child.transform.HorizontalPercents = 0f;

					current += gapSize;
				}
			}
		}
	}
}