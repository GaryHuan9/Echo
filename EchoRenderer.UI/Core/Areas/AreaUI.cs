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

		public void Add(AreaUI child)
		{
			if (Contains(child)) return;

			child.Parent?.Remove(child);
			child.transform.MarkDirty();

			children.Add(child);
			child.Parent = this;
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
			float _downPercent;
			float _leftPercent;
			float _upPercent;

			public float RightPercent
			{
				get => _rightPercent;
				set => Assign(ref _rightPercent, value);
			}

			public float DownPercent
			{
				get => _downPercent;
				set => Assign(ref _downPercent, value);
			}

			public float LeftPercent
			{
				get => _leftPercent;
				set => Assign(ref _leftPercent, value);
			}

			public float UpPercent
			{
				get => _upPercent;
				set => Assign(ref _upPercent, value);
			}

			float _rightMargin;
			float _downMargin;
			float _leftMargin;
			float _upMargin;

			public float RightMargin
			{
				get => _rightMargin;
				set => Assign(ref _rightMargin, value);
			}

			public float DownMargin
			{
				get => _downMargin;
				set => Assign(ref _downMargin, value);
			}

			public float LeftMargin
			{
				get => _leftMargin;
				set => Assign(ref _leftMargin, value);
			}

			public float UpMargin
			{
				get => _upMargin;
				set => Assign(ref _upMargin, value);
			}

			public float UniformPercent
			{
				get => (RightPercent + DownPercent + LeftPercent + UpPercent) / 4f;
				set => RightPercent = DownPercent = LeftPercent = UpPercent = value;
			}

			public float UniformMargin
			{
				get => (RightMargin + DownMargin + LeftMargin + UpMargin) / 4f;
				set => RightMargin = DownMargin = LeftMargin = UpMargin = value;
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
										  UpPercent
									  ) + new Float2
									  (
										  LeftMargin,
										  UpMargin
									  );

					Float2 size = parentSize * new Float2
								  (
									  1f - RightPercent - LeftPercent,
									  1f - UpPercent - DownPercent
								  ) - new Float2
								  (
									  RightMargin + LeftMargin,
									  UpMargin + DownMargin
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