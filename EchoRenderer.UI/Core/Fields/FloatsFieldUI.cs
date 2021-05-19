using System;
using EchoRenderer.UI.Core.Areas;

namespace EchoRenderer.UI.Core.Fields
{
	public class FloatsFieldUI : AutoLayoutAreaUI
	{
		public FloatsFieldUI(int length)
		{
			Horizontal = true;
			Margins = false;

			values = new Values(this);
			fields = new FloatFieldUI[length];

			for (int i = 0; i < length; i++)
			{
				var field = new FloatFieldUI();

				field.OnValueChangedMethods += OnValueChanged;

				Add(fields[i] = field);
			}
		}

		public readonly Values values;
		public int Length => fields.Length;

		public event Action<FloatsFieldUI> OnValuesChangedMethods;

		readonly FloatFieldUI[] fields;

		void OnValueChanged(FloatFieldUI field)
		{
			OnValuesChangedMethods?.Invoke(this);
		}

		public class Values
		{
			public Values(FloatsFieldUI field) => this.field = field;

			readonly FloatsFieldUI field;

			public float this[int index]
			{
				get => field.fields[index].Value;
				set => field.fields[index].Value = value;
			}
		}
	}
}