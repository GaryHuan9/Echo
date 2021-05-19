using System;
using CodeHelpers.Mathematics;
using SFML.Graphics;
using SFML.System;

namespace EchoRenderer.UI.Core.Areas
{
	public class LabelUI : AreaUI
	{
		string _text = "";
		Text.Styles _styles;
		Alignment _align;

		public ReadOnlySpan<char> Text
		{
			get => _text;
			set
			{
				if (_text == value) return;
				_text = new string(value);

				display.DisplayedString = _text;
				transform.MarkDirty();
			}
		}

		public Text.Styles Styles
		{
			get => _styles;
			set
			{
				if (_styles == value) return;
				_styles = value;

				display.Style = value;
				transform.MarkDirty();
			}
		}

		public Alignment Align
		{
			get => _align;
			set
			{
				if (_align == value) return;
				_align = value;

				transform.MarkDirty();
			}
		}

		public override Color FillColor
		{
			get => display.FillColor;
			set => display.FillColor = value;
		}

		float LeftPadding
		{
			get
			{
				if (Align == Alignment.left) return Margin;
				FloatRect bounds = display.GetLocalBounds();

				float width = bounds.Width;
				float area = Size.x;

				switch (Align)
				{
					case Alignment.center: return area / 2f - width / 2f;
					case Alignment.right:  return area - width - Margin;
				}

				return 0f;
			}
		}

		static float Margin => Theme.SmallMargin;

		readonly Text display = new Text {Font = mono, FillColor = Theme.Current.ContrastColor};
		static readonly Font mono = new Font("Assets/Fonts/JetBrainsMono/JetBrainsMono-Bold.ttf");

		public float GetPosition(int index) => display.FindCharacterPos((uint)index).X + LeftPadding;

		public int GetIndex(float position)
		{
			position -= LeftPadding;
			int count = Text.Length;

			//Estimate index based on width
			float width = display.GetLocalBounds().Width;
			int index = (position / width * count).Round();

			//Shift index to correct position
			float head = GetDistance(index + 1);
			float tail = GetDistance(index - 1);

			int direction = head < tail ? 1 : -1;

			float current = GetDistance(index);
			float search = direction > 0 ? head : tail;

			//Continuously lower distance
			while (search < current)
			{
				index += direction;

				current = search;
				search = GetDistance(index + direction);
			}

			return index;

			float GetDistance(int target) => Math.Abs(display.FindCharacterPos((uint)target).X - position);
		}

		protected override void Reorient(Float2 position, Float2 size)
		{
			base.Reorient(position, size);

			display.CharacterSize = (uint)Math.Max(0, size.y);
			FloatRect bounds = display.GetLocalBounds();

			float margin = bounds.Left;
			float extend = bounds.Width / 2f;

			float xOrigin = 0f;
			float xPosition = 0f;

			switch (Align)
			{
				case Alignment.center:
				{
					xOrigin = margin + extend;
					xPosition = position.x + size.x / 2f;

					break;
				}
				case Alignment.left:
				{
					xOrigin = margin;
					xPosition = position.x + Margin;

					break;
				}
				case Alignment.right:
				{
					xOrigin = margin + extend * 2f;
					xPosition = position.x + size.x - Margin;

					break;
				}
			}

			float y = position.y - size.y * 0.16f;

			display.Origin = new Vector2f(xOrigin, 0f);
			display.Position = new Vector2f(xPosition, y);
		}

		protected override void Paint(RenderTarget renderTarget)
		{
			base.Paint(renderTarget);
			renderTarget.Draw(display);
		}

		public enum Alignment
		{
			center,
			left,
			right
		}
	}
}