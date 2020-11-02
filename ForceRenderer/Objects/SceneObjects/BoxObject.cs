using System;
using CodeHelpers.Vectors;
using ForceRenderer.Mathematics;
using ForceRenderer.Renderers;

namespace ForceRenderer.Objects.SceneObjects
{
	public class BoxObject : SceneObject
	{
		public BoxObject(Material material, Float3 size) : base(material) => Size = size;

		Float3 extend;

		public Float3 Size
		{
			get => extend * 2;
			set => extend = value / 2f;
		}

		public override float GetRawIntersection(in Ray ray)
		{
			Float3 m = -1f / ray.direction;
			Float3 n = m * ray.origin;
			Float3 k = m.Absoluted * extend;

			Float3 point1 = n - k;
			Float3 point2 = n + k;

			float near = Math.Max(Math.Max(point1.x, point1.y), point1.z);
			float far = Math.Min(Math.Min(point2.x, point2.y), point2.z);

			return near > far || far < 0f ? float.PositiveInfinity : near;
		}

		public override Float3 GetRawNormal(Float3 point)
		{
			int index = (point.Absoluted / extend).MaxIndex;
			return Float3.Create(index, point[index].Sign());
		}
	}
}