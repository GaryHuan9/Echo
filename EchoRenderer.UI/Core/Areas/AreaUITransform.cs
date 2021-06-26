using CodeHelpers.Mathematics;
using SFML.Graphics;

namespace EchoRenderer.UI.Core.Areas
{
	public partial class AreaUI
	{
		public class Transform
		{
			public Transform(AreaUI areaUi) => areaUI = areaUi;

			readonly AreaUI areaUI;

			float _rightPercent;
			float _bottomPercent;
			float _leftPercent;
			float _topPercent;

			public float RightPercent
			{
				get => _rightPercent;
				set => Assign(ref _rightPercent, value);
			}

			public float BottomPercent
			{
				get => _bottomPercent;
				set => Assign(ref _bottomPercent, value);
			}

			public float LeftPercent
			{
				get => _leftPercent;
				set => Assign(ref _leftPercent, value);
			}

			public float TopPercent
			{
				get => _topPercent;
				set => Assign(ref _topPercent, value);
			}

			float _rightMargin;
			float _bottomMargin;
			float _leftMargin;
			float _topMargin;

			public float RightMargin
			{
				get => _rightMargin;
				set => Assign(ref _rightMargin, value);
			}

			public float BottomMargin
			{
				get => _bottomMargin;
				set => Assign(ref _bottomMargin, value);
			}

			public float LeftMargin
			{
				get => _leftMargin;
				set => Assign(ref _leftMargin, value);
			}

			public float TopMargin
			{
				get => _topMargin;
				set => Assign(ref _topMargin, value);
			}

			public float VerticalPercents
			{
				get => (BottomPercent + TopPercent) / 2f;
				set => BottomPercent = TopPercent = value;
			}

			public float VerticalMargins
			{
				get => (BottomMargin + TopMargin) / 2f;
				set => BottomMargin = TopMargin = value;
			}

			public float HorizontalPercents
			{
				get => (RightPercent + LeftPercent) / 2f;
				set => RightPercent = LeftPercent = value;
			}

			public float HorizontalMargins
			{
				get => (RightMargin + LeftMargin) / 2f;
				set => RightMargin = LeftMargin = value;
			}

			public float UniformPercents
			{
				get => (RightPercent + BottomPercent + LeftPercent + TopPercent) / 4f;
				set => RightPercent = BottomPercent = LeftPercent = TopPercent = value;
			}

			public float UniformMargins
			{
				get => (RightMargin + BottomMargin + LeftMargin + TopMargin) / 4f;
				set => RightMargin = BottomMargin = LeftMargin = TopMargin = value;
			}

			bool _dirtied;

			public bool Dirtied
			{
				get => _dirtied;
				private set
				{
					if (value && !_dirtied)
					{
						//Dirtying the parent dirties the entire hierarchy
						//However note that if a parent is already dirtied, but the child just cleaned its transform
						//dirtying the parent again will not re-dirty the child. We only dirty the entire hierarchy
						//if there has been a modification to the dirtied boolean to reduce overhead. This behavior can be changed if necessary.

						foreach (AreaUI child in areaUI.LoopForward()) child.transform.Dirtied = true;
					}

					_dirtied = value;
				}
			}

			public void Reorient()
			{
				if (!Dirtied) return;

				Dirtied = false;

				if (areaUI.Parent == null) return;

				RectangleShape parent = areaUI.Parent.panel;

				Float2 parentPosition = parent.Position.As();
				Float2 parentSize = parent.Size.As();

				Float2 position = parentPosition + parentSize * new Float2
								  (
									  LeftPercent,
									  TopPercent
								  ) + new Float2
								  (
									  LeftMargin,
									  TopMargin
								  );

				Float2 size = parentSize * new Float2
							  (
								  1f - RightPercent - LeftPercent,
								  1f - TopPercent - BottomPercent
							  ) - new Float2
							  (
								  RightMargin + LeftMargin,
								  TopMargin + BottomMargin
							  );

				areaUI.Reorient(position, size);
			}

			public void MarkDirty() => Dirtied = true;

			void Assign(ref float original, float value)
			{
				if (!original.AlmostEquals(value)) MarkDirty();
				original = value;
			}

			public static Float2 operator *(Float2 value, Transform transform) => transform.areaUI.panel.Transform.TransformPoint(value.As()).As();
			public static Float2 operator /(Float2 value, Transform transform) => transform.areaUI.panel.InverseTransform.TransformPoint(value.As()).As();
		}
	}
}