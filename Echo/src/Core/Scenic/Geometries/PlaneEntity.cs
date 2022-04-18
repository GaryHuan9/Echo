using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Packed;
using Echo.Core.Scenic.Preparation;

namespace Echo.Core.Scenic.Geometries;

public class PlaneEntity : GeometryEntity
{
	public Float2 Size { get; set; } = Float2.One;

	public Float2 Texcoord00 { get; set; } = Float2.Zero;
	public Float2 Texcoord01 { get; set; } = Float2.Right;
	public Float2 Texcoord10 { get; set; } = Float2.Up;
	public Float2 Texcoord11 { get; set; } = Float2.One;

	// public void SetNormal(Float3 normal)
	// {
	// 	Float3 one = Float3.up;
	// 	Float3 two = normal.Normalized;
	//
	// 	Float3 cross = one.Cross(two);
	// 	float dot = one.Dot(two) + 1f;
	//
	// 	Float4 quaternion = new Float4(cross.x, cross.y, cross.z, dot);
	// 	Float4x4 rotation = Float4x4.Rotation(quaternion);
	// }

	public override IEnumerable<PreparedTriangle> ExtractTriangles(SwatchExtractor extractor)
	{
		Float2 extend = Size / 2f;

		Float3 point00 = LocalToWorld.MultiplyPoint(new Float3(-extend.X, 0f, -extend.Y));
		Float3 point01 = LocalToWorld.MultiplyPoint(new Float3(-extend.X, 0f, extend.Y));
		Float3 point10 = LocalToWorld.MultiplyPoint(new Float3(extend.X, 0f, -extend.Y));
		Float3 point11 = LocalToWorld.MultiplyPoint(new Float3(extend.X, 0f, extend.Y));

		MaterialIndex material = extractor.Register(Material, 2);

		yield return new PreparedTriangle(point00, point11, point10, Texcoord00, Texcoord11, Texcoord10, material);
		yield return new PreparedTriangle(point00, point01, point11, Texcoord00, Texcoord01, Texcoord11, material);
	}

	public override IEnumerable<PreparedSphere> ExtractSpheres(SwatchExtractor extractor) => Enumerable.Empty<PreparedSphere>();
}