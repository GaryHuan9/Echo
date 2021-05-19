using System;

namespace EchoRenderer.UI.Core.Areas
{
	public class SliderUI : PressableUI
	{
		public SliderUI()
		{
			label = new LabelUI {transform = {UniformMargins = Theme.SmallMargin}};
			Add(label);
		}

		public event Action<float> OnSlideMethods;

		readonly LabelUI label;

		protected override void OnMousePressed() { }
	}
}