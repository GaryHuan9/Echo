﻿using System;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Objects.Instancing
{
	public class ObjectPack : Object
	{
		public override Float3 Position
		{
			set
			{
				if (value.EqualsExact(Position)) return;
				ThrowModifyTransformException();
			}
		}

		public override Float3 Rotation
		{
			set
			{
				if (value.EqualsExact(Rotation)) return;
				ThrowModifyTransformException();
			}
		}

		public override Float3 Scale
		{
			set
			{
				if (value.EqualsExact(Scale)) return;
				ThrowModifyTransformException();
			}
		}

		/// <summary>
		/// The maximum number of instanced layers allowed (excluding the root).
		/// Can be increased if needed at a performance and memory penalty.
		/// </summary>
		public const int MaxLayer = 5;

		static void ThrowModifyTransformException() => throw new Exception($"Cannot modify {nameof(ObjectPack)} transform!");
	}
}