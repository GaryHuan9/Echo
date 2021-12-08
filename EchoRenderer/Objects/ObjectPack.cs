using System;
using CodeHelpers.Mathematics;

namespace EchoRenderer.Objects
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

		/// <summary>
		/// The maximum number of instanced layers allowed (excluding the root).
		/// This number can be increased if needed at a performance penalty.
		/// </summary>
		public const int MaxLayer = 6;

		static void ThrowModifyTransformException() => throw new Exception($"Cannot modify {nameof(ObjectPack)} transform!");
	}
}