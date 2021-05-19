using System;
using System.Text;
using EchoRenderer.UI.Core.Areas;
using EchoRenderer.UI.Core.Interactions;
using SFML.Window;

namespace EchoRenderer.UI.Core.Fields
{
	public class TextFieldUI : PressableUI
	{
		public TextFieldUI()
		{
			label = new LabelUI {transform = {UniformMargins = Theme.SmallMargin}};
			cursor = new AreaUI {FillColor = Theme.SpecialColor};

			Add(label);
			label.Add(cursor);

			UpdateCursorTransform();
		}

		readonly LabelUI label;
		readonly AreaUI cursor;

		readonly StringBuilder builder = new StringBuilder();

		bool _editing;
		int _cursorPosition;

		bool Editing
		{
			get => _editing;
			set
			{
				if (_editing == value) return;
				_editing = value;

				//TODO: update cursor
			}
		}

		int CursorPosition
		{
			get => _cursorPosition;
			set
			{
				if (_cursorPosition == value) return;

				_cursorPosition = value;
				UpdateCursorTransform();
			}
		}

		public override void Update()
		{
			base.Update();

			IHoverable pressing = Root.MousePressing;
			if (pressing != null && pressing != this) Editing = false;
		}

		protected override void OnMousePressed()
		{
			Editing = true;
		}

		void OnTextEntered(object sender, TextEventArgs args)
		{
			if (!Editing) return;
			char code = args.Unicode[0];

			if (!char.IsControl(code))
			{
				builder.Insert(CursorPosition++, code);
				label.Text = builder.ToString();
			}
		}

		void OnKeyPressed(object sender, KeyEventArgs args)
		{
			switch (args.Code)
			{
				case Keyboard.Key.Escape:
				case Keyboard.Key.Enter:
				{
					Editing = false;
					break;
				}
				case Keyboard.Key.Backspace:
				{
					if (CursorPosition == 0) break;

					builder.Remove(--CursorPosition, 1);
					label.Text = builder.ToString();

					break;
				}
				case Keyboard.Key.Delete:
				{
					if (CursorPosition == builder.Length) break;

					builder.Remove(CursorPosition, 1);
					label.Text = builder.ToString();

					break;
				}
				case Keyboard.Key.Left:
				{
					CursorPosition = Math.Max(CursorPosition - 1, 0);
					break;
				}
				case Keyboard.Key.Right:
				{
					CursorPosition = Math.Min(CursorPosition + 1, builder.Length);
					break;
				}
			}
		}

		void UpdateCursorTransform()
		{
			float position = label.GetPosition(CursorPosition);
			const float Thickness = 1f;

			cursor.transform.VerticalPercents = 0f;
			cursor.transform.VerticalMargins = 0f;

			cursor.transform.LeftPercent = 0f;
			cursor.transform.RightPercent = 1f;

			cursor.transform.LeftMargin = -Thickness + position;
			cursor.transform.RightMargin = -Thickness - position;
		}

		protected override void OnRootChanged(RootUI previous)
		{
			base.OnRootChanged(previous);

			if (previous != null)
			{
				previous.application.TextEntered -= OnTextEntered;
				previous.application.KeyPressed -= OnKeyPressed;
			}

			if (Root != null)
			{
				Root.application.TextEntered += OnTextEntered;
				Root.application.KeyPressed += OnKeyPressed;
			}
		}
	}
}