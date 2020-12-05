using CodeHelpers.Vectors;
using ForceRenderer.CodeHelpers.Vectors;
using ForceRenderer.IO;
using ForceRenderer.Objects.SceneObjects;
using Object = ForceRenderer.Objects.Object;

namespace ForceRenderer
{
	public class Scene : Object
	{
		public Cubemap Cubemap { get; set; }

		public void PlacePlane(Float3 origin, Float3 normal, Float2 size, Material material)
		{
			Float3 one = Float3.up;
			Float3 two = normal.Normalized;

			Float3 cross = one.Cross(two);
			float dot = one.Dot(two) + 1f;

			Float4 quaternion = new Float4(cross.x, cross.y, cross.z, dot);
			Float4x4 rotation = Float4x4.Rotation(quaternion);

			Float3 point00 = rotation.MultiplyPoint(new Float3(-size.x, 0f, -size.y)) + origin;
			Float3 point01 = rotation.MultiplyPoint(new Float3(-size.x, 0f, size.y)) + origin;
			Float3 point10 = rotation.MultiplyPoint(new Float3(size.x, 0f, -size.y)) + origin;
			Float3 point11 = rotation.MultiplyPoint(new Float3(size.x, 0f, size.y)) + origin;

			children.Add(new TriangleObject(material, point00, point11, point10));
			children.Add(new TriangleObject(material, point00, point01, point11));
		}
	}
}