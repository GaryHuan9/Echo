using System;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;

namespace Echo.Core.Scenic.Instancing;

public class PackInstance : Entity
{
	public override Float3 Scale
	{
		get => base.Scale;
		set
		{
			if (value.X.AlmostEquals(value.Y) && value.X.AlmostEquals(value.Z)) base.Scale = (Float3)value.Average;
			else throw new Exception($"Cannot use none uniformed scale of '{value}' for {nameof(PackInstance)}!");
		}
	}

	public EntityPack EntityPack { get; set; }
	public MaterialSwatch Swatch { get; set; }
}