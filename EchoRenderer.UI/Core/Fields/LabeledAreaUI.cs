using EchoRenderer.UI.Core.Areas;

namespace EchoRenderer.UI.Core.Fields
{
	public class LabeledAreaUI : AreaUI
	{
		public LabeledAreaUI()
		{
			var left = new AreaUI
					   {
						   transform =
						   {
							   UniformPercents = 0f,
							   UniformMargins = 0f,
							   RightPercent = 0.6f,
							   RightMargin = Theme.LargeMargin
						   },
						   PanelColor = Theme.PanelColor
					   };

			label = new LabelUI
					{
						transform =
						{
							UniformPercents = 0f,
							UniformMargins = Theme.SmallMargin,
							LeftMargin = Theme.LargeMargin
						},
						Align = LabelUI.Alignment.left
					};

			Add(left);
			left.Add(label);
		}

		AreaUI _area;

		public AreaUI Area
		{
			get => _area;
			set
			{
				if (_area == value) return;
				_area = value;

				if (value != null)
				{
					Add(value);

					value.transform.UniformPercents = 0f;
					value.transform.UniformMargins = 0f;

					value.transform.LeftPercent = 0.4f;
				}
			}
		}

		public readonly LabelUI label;
	}
}