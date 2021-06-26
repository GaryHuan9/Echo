namespace EchoRenderer.UI.Core.Areas
{
	public class AutoLayoutAreaUI : AreaUI, ILayoutAreaUI
	{
		public bool Horizontal { get; set; }

		public bool Margins { get; set; } = true;
		public bool Gaps { get; set; } = true;

		//Additional margin sizes used on minor axes
		public float PositiveMargin { get; set; }
		public float NegativeMargin { get; set; }

		public float PreferedHeight { get; private set; }

		public override void Update()
		{
			base.Update();

			PreferedHeight = 0f;

			int count = ChildCount;
			if (count == 0) return;

			float marginSize = Margins ? Theme.MediumMargin : 0f;
			float gapSize = Gaps ? Theme.MediumMargin : 0f;

			if (Horizontal)
			{
				float gap = gapSize / Dimension.x;
				float margin = marginSize / Dimension.x;

				float total = 1f - margin * 2f - (count - 1) * gap;

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

					current += gap;
				}
			}
			else
			{
				float total = Dimension.y;
				float current = marginSize;

				foreach (AreaUI child in LoopForward())
				{
					float height = Theme.LayoutHeight;
					Transform target = child.transform;

					if (child is ILayoutAreaUI layout) height = layout.PreferedHeight;

					//Vertical layouts are solely controlled by margins
					target.TopMargin = current;
					current += height;
					target.BottomMargin = total - current;

					target.VerticalPercents = 0f;
					target.HorizontalPercents = 0f;

					target.RightMargin = marginSize + PositiveMargin;
					target.LeftMargin = marginSize + NegativeMargin;

					current += gapSize;
				}

				PreferedHeight = current + marginSize - gapSize;
			}
		}
	}

	public interface ILayoutAreaUI
	{
		float PreferedHeight { get; }
	}
}