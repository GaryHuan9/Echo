using System;
using CodeHelpers.Mathematics;

namespace ForceRenderer.Objects
{
	public class ObjectPack : Object
	{
		public override Float3 Position
		{
			set
			{
				if (value == Position) return;
				ThrowModifyTransformException();
			}
		}

		public override Float3 Rotation
		{
			set
			{
				if (value == Rotation) return;
				ThrowModifyTransformException();
			}
		}

		public override Float3 Scale
		{
			set
			{
				if (value == Scale) return;
				ThrowModifyTransformException();
			}
		}

		static void ThrowModifyTransformException() => throw new Exception($"Cannot modify {nameof(ObjectPack)} transform!");
	}
}