using System;
using CodeHelpers.Mathematics;
using ForceRenderer.Objects.GeometryObjects;
using ForceRenderer.Rendering.Materials;

namespace ForceRenderer.Mathematics.Intersections
{
	public class GeometryPack
	{
		readonly BoundingVolumeHierarchy bvh;
		readonly Material[] materials;

		readonly PressedTriangle[] triangles;      //Indices: [0x8000_0000 to 0xFFFF_FFFF)
		readonly PressedSphere[] spheres;          //Indices [0x4000_0000 to 0x8000_0000)
		readonly GeometryPackInstance[] instances; //Indices [0 to 0x4000_0000)

		const uint TrianglesTreshold = 0x8000_0000u;
		const uint SpheresTreshold = 0x4000_0000u;

		/// <summary>
		/// Calculates the intersection between <paramref name="ray"/> and object with <paramref name="token"/>.
		/// If the intersection occurs before the original <paramref name="ray.distance"/>, then the intersection is recorded.
		/// </summary>
		public void GetIntersection(in Ray ray, ref Hit hit, uint token)
		{
			switch (token)
			{
				case >= TrianglesTreshold:
				{
					ref PressedTriangle triangle = ref triangles[token - TrianglesTreshold];
					float distance = triangle.GetIntersection(ray, out Float2 uv);

					if (distance >= hit.distance) return;
					hit = new Hit(this, distance, token, uv);

					break;
				}
				case >= SpheresTreshold:
				{
					ref PressedSphere sphere = ref spheres[token - SpheresTreshold];
					float distance = sphere.GetIntersection(ray, out Float2 uv);

					if (distance >= hit.distance) return;
					hit = new Hit(this, distance, token, uv);

					break;
				}
				default:
				{
					instances[token].GetIntersection(ray, ref hit);
					break;
				}
			}
		}

		/// <summary>
		/// Creates a <see cref="CalculatedHit"/> from <paramref name="hit"/> and <paramref name="ray"/> of this pack.
		/// NOTE: The method operates in this <see cref="GeometryPack"/>'s local coordinate space, which means that
		/// both the input <paramref name="ray"/> and the output hit must be converted.
		/// </summary>
		public CalculatedHit CreateHit(in Hit hit, in Ray ray)
		{
			int materialToken;
			Float3 normal;
			Float2 texcoord;

			switch (hit.token)
			{
				case >= TrianglesTreshold:
				{
					ref PressedTriangle triangle = ref triangles[hit.token - TrianglesTreshold];

					materialToken = triangle.materialToken;
					normal = triangle.GetNormal(hit.uv);
					texcoord = triangle.GetTexcoord(hit.uv);

					break;
				}
				case >= SpheresTreshold:
				{
					ref PressedSphere sphere = ref spheres[hit.token - SpheresTreshold];

					materialToken = sphere.materialToken;
					normal = sphere.GetNormal(hit.uv);
					texcoord = hit.uv; //Sphere directly uses the uv as texcoord

					break;
				}
				default: throw new Exception($"{nameof(CreateHit)} should be invoked on the base {nameof(GeometryPack)}, which is {hit.instance}!");
			}

			return new CalculatedHit
			(
				ray.GetPoint(hit.distance), ray.direction, hit.distance,
				materials[materialToken], normal, texcoord
			);
		}
	}
}