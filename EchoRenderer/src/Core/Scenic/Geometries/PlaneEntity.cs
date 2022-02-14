using System.Collections.Generic;
using System.Linq;
using CodeHelpers.Mathematics;
using EchoRenderer.Core.Rendering.Materials;
using EchoRenderer.Core.Scenic.Preparation;

namespace EchoRenderer.Core.Scenic.Geometries;

public class PlaneEntity : GeometryEntity
{
	public PlaneEntity(Material material, Float2 size) : base(material) => Size = size;

	public Float2 Size { get; set; }

	public Float2 Texcoord00 { get; set; } = Float2.zero;
	public Float2 Texcoord01 { get; set; } = Float2.right;
	public Float2 Texcoord10 { get; set; } = Float2.up;
	public Float2 Texcoord11 { get; set; } = Float2.one;

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

		Float3 point00 = LocalToWorld.MultiplyPoint(new Float3(-extend.x, 0f, -extend.y));
		Float3 point01 = LocalToWorld.MultiplyPoint(new Float3(-extend.x, 0f, extend.y));
		Float3 point10 = LocalToWorld.MultiplyPoint(new Float3(extend.x, 0f, -extend.y));
		Float3 point11 = LocalToWorld.MultiplyPoint(new Float3(extend.x, 0f, extend.y));

		uint materialToken = extractor.Register(Material);

		yield return new PreparedTriangle(point00, point11, point10, Texcoord00, Texcoord11, Texcoord10, materialToken);
		yield return new PreparedTriangle(point00, point01, point11, Texcoord00, Texcoord01, Texcoord11, materialToken);
	}

	public override IEnumerable<PreparedSphere> ExtractSpheres(SwatchExtractor extractor) => Enumerable.Empty<PreparedSphere>();
}