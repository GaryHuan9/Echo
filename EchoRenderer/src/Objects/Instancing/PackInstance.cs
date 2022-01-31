﻿using System;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Objects.Instancing
{
	public class PackInstance : Object
	{
		public override Float3 Scale
		{
			get => base.Scale;
			set
			{
				if (value.x.AlmostEquals(value.y) && value.x.AlmostEquals(value.z)) base.Scale = (Float3)value.Average;
				else throw new Exception($"Cannot use none uniformed scale of '{value}' for {nameof(PackInstance)}!");
			}
		}

		public ObjectPack ObjectPack { get; set; }
		public MaterialSwatch Swatch { get; set; }
	}
}