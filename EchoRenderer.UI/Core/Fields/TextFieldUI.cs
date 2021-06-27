using System;
using CodeHelpers.Collections;
using CodeHelpers.Mathematics;
using EchoRenderer.UI.Core.Areas;
using EchoRenderer.UI.Core.Interactions;
using SFML.Window;

namespace EchoRenderer.UI.Core.Fields
{
	public class TextFieldUI : PressableUI
	{
		public TextFieldUI()
		{
			cursor = new CursorUI(this) {PanelColor = Theme.SpecialColor};

			Add(currentDisplay);
			Add(editingDisplay);

			editingDisplay.Add(cursor);
		}

		public ReadOnlySpan<char> Text
		{
			get => currentBuffer.Value;
			set
			{
				if (value.SequenceEqual(Text)) return;

				currentBuffer.Value = value;
				currentDisplay.Text = value;

				OnTextChangedMethods?.Invoke(this);
			}
		}

		public int Length => currentBuffer.Length;

		public event Action<TextFieldUI> OnTextChangedMethods;

		readonly LabelUI currentDisplay = new LabelUI {transform = {UniformMargins = Theme.SmallMargin}, Align = LabelUI.Alignment.center};
		readonly LabelUI editingDisplay = new LabelUI {transform = {UniformMargins = Theme.SmallMargin}, Align = LabelUI.Alignment.left};

		readonly CursorUI cursor;

		readonly CharBuffer currentBuffer = new CharBuffer();
		readonly CharBuffer editingBuffer = new CharBuffer();

		bool _editing;

		bool Editing
		{
			get => _editing;
			set
			{
				if (_editing == value) return;

				_editing = value;
				cursor.Enabled = value;

				currentDisplay.Visible = !value;
				editingDisplay.Visible = value;

				if (value)
				{
					editingBuffer.Value = currentBuffer.Value;
					editingDisplay.Text = currentBuffer.Value;

					cursor.ClampPosition();
				}
				else Text = editingBuffer.Value;
			}
		}

		public override void Update()
		{
			base.Update();

			IHoverable pressing = Root.MousePressing;
			if (pressing != null && pressing != this) Editing = false;
		}

		public override void OnMousePressed(MousePress mouse)
		{
			base.OnMousePressed(mouse);

			float x = mouse.point.x - editingDisplay.Position.x;
			cursor.Position = editingDisplay.GetIndex(x);
		}

		protected override void OnMousePressed() => Editing = true;

		void OnTextEntered(object sender, TextEventArgs args)
		{
			if (!Editing) return;
			char code = args.Unicode[0];

			if (!char.IsControl(code))
			{
				editingBuffer.Insert(cursor.Position, code);
				editingDisplay.Text = editingBuffer.Value;

				++cursor.Position;
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
					if (cursor.Position == 0) break;

					editingBuffer.Remove(--cursor.Position);
					editingDisplay.Text = editingBuffer.Value;

					break;
				}
				case Keyboard.Key.Delete:
				{
					if (cursor.Position == editingBuffer.Length) break;

					editingBuffer.Remove(cursor.Position);
					editingDisplay.Text = editingBuffer.Value;

					break;
				}
				case Keyboard.Key.Left:
				{
					--cursor.Position;
					break;
				}
				case Keyboard.Key.Right:
				{
					++cursor.Position;
					break;
				}
			}
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

		class CharBuffer
		{
			public int Length { get; private set; }

			public ReadOnlySpan<char> Value
			{
				get => new(array, 0, Length);
				set
				{
					EnsureCapacity(value.Length);

					Length = value.Length;
					value.CopyTo(array);
				}
			}

			char[] array = new char[16];

			public void Insert(int index, char value)
			{
				EnsureCapacity(Length + 1);
				array.Insert(index, value);

				++Length;
			}

			public void Remove(int index)
			{
				--Length;
				for (int i = index; i < Length; i++) array[i] = array[i + 1];
			}

			void EnsureCapacity(int length)
			{
				int resize = array.Length;
				if (length <= resize) return;

				while (resize < length) resize *= 2;

				char[] newArray = new char[resize];
				Array.Copy(array, newArray, Length);

				array = newArray;
			}
		}

		class CursorUI : AreaUI
		{
			public CursorUI(TextFieldUI field) => this.field = field;

			readonly TextFieldUI field;

			double blinkStart;

			int _position;
			bool _enabled;

			public int Position
			{
				get => _position;
				set
				{
					if (_position == value) return;

					_position = value;
					transform.MarkDirty();

					ResetBlink();
					ClampPosition();
				}
			}

			public bool Enabled
			{
				get => _enabled;
				set
				{
					if (_enabled == value) return;

					_enabled = value;
					transform.MarkDirty();
					ResetBlink();
				}
			}

			const float BlinkDelay = 0.53f;
			const float Thickness = 1f;

			public override void Update()
			{
				base.Update();

				if (transform.Dirtied)
				{
					transform.VerticalPercents = 0f;
					transform.VerticalMargins = 0f;

					transform.LeftPercent = 0f;
					transform.RightPercent = 1f;

					float x = field.editingDisplay.GetPosition(Position);

					transform.LeftMargin = -Thickness + x;
					transform.RightMargin = -Thickness - x;
				}

				double delay = Root.application.TotalTime - blinkStart;
				bool visible = (int)(delay / BlinkDelay) % 2 == 0;

				Visible = visible && Enabled;
			}

			public void ClampPosition() => Position = Position.Clamp(0, field.editingBuffer.Length);

			protected override void OnRootChanged(RootUI previous)
			{
				base.OnRootChanged(previous);
				if (Root != null) ResetBlink();
			}

			void ResetBlink() => blinkStart = Root.application.TotalTime;
		}
	}
}