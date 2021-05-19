using EchoRenderer.UI.Core.Areas;

namespace EchoRenderer.UI.Core.Fields
{
	public abstract class LabeledFieldUI : AreaUI
	{
		protected LabeledFieldUI()
		{
			label = new LabelUI();

			Add(label);
		}

		protected AreaUI Field { get; set; }
		public readonly LabelUI label;

		public override void Update()
		{
			base.Update();

			if (Field != null)
			{
				label.transform.LeftPercent = 0.4f;
				label.transform.RightPercent = 0f;

				label.transform.VerticalPercents = 0f;
				label.transform.UniformMargins = 0f;
			}

			label.transform.LeftPercent = 0f;
			label.transform.RightPercent = 0.6f;

			label.transform.VerticalPercents = 0f;
			label.transform.UniformMargins = 0f;
		}
	}
}