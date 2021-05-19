using System;
using System.Globalization;
using CodeHelpers.Mathematics;
using SFML.Window;

namespace EchoRenderer.UI.Core.Fields
{
	public class FloatFieldUI : TextFieldUI
	{
		public FloatFieldUI()
		{
			OnTextChangedMethods += OnTextChanged;

			ResetFormat();
			ResetText();

			Increment = 0f;
		}

		float _value;
		float _increment;

		public float Increment
		{
			get => _increment;
			set
			{
				if (Scalars.AlmostEquals(_increment, value) || value < 0f) return;

				_increment = value;
				Value = _value;

				ResetFormat();
				ResetText();
			}
		}

		public float Value
		{
			get => _value;
			set
			{
				if (!Scalars.AlmostEquals(Increment, 0f))
				{
					float remain = value.Repeat(Increment);
					if (remain > Increment / 2f) value += Increment;

					value -= remain;
				}

				if (Scalars.AlmostEquals(_value, value)) return;

				_value = value;
				ResetText();
			}
		}

		string format;

		const float DefaultIncrement = .1f;
		const float ModifyMultiplier = 10f;
		const int FormatMaxDecimal = 3;

		public override void OnMouseScrolled(Float2 delta)
		{
			base.OnMouseScrolled(delta);
			float change = delta.Sum;

			change *= Scalars.AlmostEquals(Increment, 0f) ? DefaultIncrement : Increment;
			if (Keyboard.IsKeyPressed(Keyboard.Key.LShift)) change /= ModifyMultiplier;
			if (Keyboard.IsKeyPressed(Keyboard.Key.LControl)) change *= ModifyMultiplier;

			Value += change;
		}

		void OnTextChanged(TextFieldUI field)
		{
			if (float.TryParse(field.Text, NumberStyles.Any, Theme.Culture, out float value))
			{
				Value = value;
			}
			else ResetText();
		}

		void ResetFormat()
		{
			string toString = Increment.ToString(CultureInfo.InvariantCulture);
			int count = toString.Length - Math.Max(toString.IndexOf('.'), 0) - 1;

			if (Scalars.AlmostEquals(Increment, 0f)) count = int.MaxValue;
			format = $"F{count.Clamp(0, FormatMaxDecimal)}";
		}

		void ResetText() => Text = Value.ToString(format, Theme.Culture);
	}
}