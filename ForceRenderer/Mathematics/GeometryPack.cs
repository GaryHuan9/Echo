using CodeHelpers.Mathematics;
using ForceRenderer.Objects.SceneObjects;
using ForceRenderer.Rendering.Materials;

namespace ForceRenderer.Mathematics
{
	public class GeometryPack
	{
		readonly BoundingVolumeHierarchy bvh;

		readonly PressedTriangle[] triangles; //Indices: [0 to int.MaxValue)
		readonly PressedSphere[] spheres;     //Indices [~0 to ~(int.MaxValue >> 1))
		readonly GeometryPack[] packs;        //Indices from [~(int.MaxValue >> 1) to ~int.MaxValue)

		readonly Material[] materials;

		const int PacksThreshold = ~(int.MaxValue >> 1);

		/// <summary>
		/// Returns the intersection between <paramref name="ray"/> and object with <paramref name="token"/>.
		/// </summary>
		public float GetIntersection(in Ray ray, int token, out Float2 uv)
		{
			if (token >= 0)
			{
				ref PressedTriangle triangle = ref triangles[token];
				return triangle.GetIntersection(ray, out uv);
			}

			if (token >= PacksThreshold)
			{
				GeometryPack pack = packs[token];


				return pack;
			}

			ref PressedSphere sphere = ref spheres[~token];
			return sphere.GetIntersection(ray, out uv);
		}

		/// <summary>
		/// Creates a <see cref="CalculatedHit"/> from <paramref name="hit"/> and <paramref name="ray"/> of this pack.
		/// </summary>
		public CalculatedHit CreateHit(in Hit hit, in Ray ray) { }
	}

	public readonly struct CalculatedHit
	{
		public CalculatedHit(in Hit hit, in Ray ray, Material material, in Float3 normal, Float2 texcoord)
		{
			position = ray.GetPoint(hit.distance);
			direction = ray.direction;
			distance = hit.distance;

			this.material = material;
			this.normal = normal;
			this.texcoord = texcoord;
		}

		public readonly Float3 position;
		public readonly Float3 direction;
		public readonly float distance;

		public readonly Material material;
		public readonly Float3 normal;
		public readonly Float2 texcoord;
	}
}