using System;
using System.Collections;
using System.Collections.Generic;
using CodeHelpers;
using CodeHelpers.Mathematics;
using SFML.Graphics;

namespace EchoRenderer.UI.Core.Areas
{
	public class AreaUI : IEnumerable<AreaUI>
	{
		public AreaUI()
		{
			transform = new Transform(this);

			Random random = RandomHelper.CurrentRandom;
			Span<byte> bytes = stackalloc byte[3];

			random.NextBytes(bytes);

			// FillColor = Color.Transparent;
			FillColor = new Color(bytes[0], bytes[1], bytes[2]);
		}

		public readonly Transform transform;

		public AreaUI Parent { get; private set; }
		public bool Visible { get; set; } = true;

		public Color FillColor
		{
			get => panel.FillColor;
			set => panel.FillColor = value;
		}

		readonly RectangleShape panel = new RectangleShape();
		readonly List<AreaUI> children = new List<AreaUI>();

		public AreaUI this[int index] => children[index];
		public int ChildCount => children.Count;

		public AreaUI Add(AreaUI child)
		{
			if (Contains(child)) return this;

			child.Parent?.Remove(child);
			child.transform.MarkDirty();

			children.Add(child);
			child.Parent = this;

			return this;
		}

		public bool Remove(AreaUI child)
		{
			if (!children.Remove(child)) return false;

			child.Parent = null;
			return true;
		}

		public bool Contains(AreaUI child) => child.Parent == this;

		public virtual void Update()
		{
			foreach (AreaUI child in this) child.Update();
		}

		public void Draw(RenderTarget renderTarget)
		{
			transform.Reorient();

			if (panel.Size.As() > Float2.zero && Visible) Paint(renderTarget);
			foreach (AreaUI child in this) child.Draw(renderTarget);
		}

		protected virtual void Reorient(Float2 position, Float2 size)
		{
			panel.Position = position.As();
			panel.Size = size.As();
		}

		protected virtual void Paint(RenderTarget renderTarget)
		{
			if (FillColor.A > 0) renderTarget.Draw(panel);
		}

		List<AreaUI>.Enumerator GetEnumerator() => children.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		IEnumerator<AreaUI> IEnumerable<AreaUI>.GetEnumerator() => GetEnumerator();

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

			public float UniformPercent
			{
				get => (RightPercent + BottomPercent + LeftPercent + TopPercent) / 4f;
				set => RightPercent = BottomPercent = LeftPercent = TopPercent = value;
			}

			public float UniformMargin
			{
				get => (RightMargin + BottomMargin + LeftMargin + TopMargin) / 4f;
				set => RightMargin = BottomMargin = LeftMargin = TopMargin = value;
			}

			bool _dirtied;

			bool Dirtied
			{
				get => _dirtied;
				set
				{
					if (value && !_dirtied)
					{
						//Dirtying the parent dirties the entire hierarchy
						//However note that if a parent is already dirtied, but the child just cleaned its transform
						//dirtying the parent again will not re-dirty the child. We only dirty the entire hierarchy
						//if there has been a modification to the dirtied boolean to reduce overhead. This behavior can be changed if necessary.

						foreach (AreaUI child in areaUI) child.transform.Dirtied = true;
					}

					_dirtied = value;
				}
			}

			public void Reorient()
			{
				if (Dirtied && areaUI.Parent != null)
				{
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

				Dirtied = false;
			}

			public void MarkDirty() => Dirtied = true;

			void Assign(ref float original, float value)
			{
				if (!Scalars.AlmostEquals(original, value)) MarkDirty();
				original = value;
			}
		}
	}
}