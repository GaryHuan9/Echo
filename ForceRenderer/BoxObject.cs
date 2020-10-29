using System;
using CodeHelpers.Vectors;

namespace ForceRenderer
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

		public override float SignedDistance(Float3 point)
		{
			Float3 q = (point - Position).Absoluted - extend;
			return q.Max(Float3.zero).Magnitude + Math.Min(Math.Max(q.x, Math.Max(q.y, q.z)), 0f);
		}
	}
}