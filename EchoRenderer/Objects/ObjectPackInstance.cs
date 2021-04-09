using System;
using System.Collections.Generic;
using CodeHelpers.Mathematics;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering.Materials;

namespace EchoRenderer.Objects
{
	public class ObjectPackInstance : Object
	{
		public ObjectPackInstance(ObjectPack objectPack = null) => ObjectPack = objectPack;

		public override Float3 Scale
		{
			get => base.Scale;
			set
			{
				if (Scalars.AlmostEquals(value.x, value.y) && Scalars.AlmostEquals(value.y, value.z)) base.Scale = (Float3)value.Average;
				else throw new Exception($"Cannot using none uniformed scale of '{value}' for {nameof(ObjectPackInstance)}!");
			}
		}

		public ObjectPack ObjectPack { get; set; }
		public MaterialMapper Mapper { get; set; }
	}
}