using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Accelerators;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Scattering;

namespace EchoRenderer.Mathematics.Intersections
{
	/// <summary>
	/// Mutable data struct used to communicate intersection information.
	/// </summary>
	public struct HitQuery
	{
		public HitQuery(in Ray ray)
		{
			this.ray = ray;
			instance = null;

			previous = default;
			token = default;

			distance = float.PositiveInfinity;
			uv = default;
		}

		HitQuery(in HitQuery last)
		{

		}

		public readonly Ray ray;

		/// <summary>
		/// Used during intersection test. Assigned to the <see cref="PressedPackInstance"/> that the
		/// query is currently travelling through and assigned to null once the test is concluded
		/// </summary>
		public PressedPackInstance instance;

		public GeometryToken previous;
		public GeometryToken token;

		public float distance;
		public Float3 normal;
		public Float2 uv;

		public Shading shading;
		public BSDF bsdf;

		public readonly bool Hit => token != default;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly HitQuery Next(Float3 direction) => new HitQuery()
														   {
															   ray = new Ray(ray.GetPoint(distance), direction),
														   };
		{
			ray = new Ray(ray.GetPoint(distance), direction);

			previous = token;
			token = default;
			bsdf = null;

			distance = float.PositiveInfinity;
		}

		public static implicit operator HitQuery(in Ray ray) => new(ray);

		/// <summary>
		/// Information calculated right after intersection calculation
		/// </summary>
		public struct Shading
		{
			public Float3 normal;
			public Float2 texcoord;
			public Material material;
		}
	}
}