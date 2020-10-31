using System;
using CodeHelpers.Vectors;

namespace ForceRenderer.Objects
{
	public class BoxObject : SceneObject
	{
		public BoxObject(Float3 size) => Size = size;

		Float3 extend;

		public Float3 Size
		{
			get => extend * 2f;
			set => extend = value / 2f;
		}

		public override float GetSignedDistanceRaw(Float3 point)
		{
			point = point.Absoluted;

			float x = point.x - extend.x;
			float y = point.y - extend.y;
			float z = point.z - extend.z;

			return new Float3(Math.Max(x, 0f), Math.Max(y, 0f), Math.Max(z, 0f)).Magnitude + Math.Min(0f, Math.Max(x, Math.Max(y, z)));
		}

		public override Float3 GetNormalRaw(Float3 point)
		{
			int index = (point.Absoluted / extend).MaxIndex;
			return Float3.Create(index, point[index].Sign());
		}
	}
}