using System;
using CodeHelpers.Packed;

namespace Echo.UserInterface.Core.Fields;

public class Float3FieldUI : FloatsFieldUI
{
	public Float3FieldUI() : base(3) => OnValuesChangedMethods += OnValuesChanged;

	public Float3 Value
	{
		get => new Float3(values[0], values[1], values[2]);
		set
		{
			values[0] = value.X;
			values[1] = value.Y;
			values[2] = value.Z;
		}
	}

	public event Action<Float3FieldUI> OnValueChangedMethods;

	void OnValuesChanged(FloatsFieldUI field)
	{
		OnValueChangedMethods?.Invoke(this);
	}
}