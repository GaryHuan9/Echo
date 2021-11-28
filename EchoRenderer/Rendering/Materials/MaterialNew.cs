using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Scattering;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.Materials
{
	using static Utilities;

	public abstract class MaterialNew
	{
		public Texture Albedo { get; set; }
		public Texture Normal { get; set; }

		public float NormalIntensity { get; set; } = 1f;

		static readonly Vector128<float> normalShift = Vector128.Create(-1f, -1f, -2f, 0f);

		public virtual void BeforeRender()
		{
			Albedo ??= Texture.black;
			Normal ??= Texture.normal;
		}

		/// <summary>
		/// Determines the scattering properties of this material at <paramref name="query"/>.
		/// Initializes a <see cref="BSDF"/>.
		/// </summary>
		public abstract void Scatter(ref HitQuery query, Arena arena);

		/// <summary>
		/// Fills in the shading normal for <paramref name="query"/> based on this <see cref="MaterialNew"/>'s <see cref="Normal"/>.
		/// </summary>
		public void FillTangentNormal(ref HitQuery query)
		{
			ref readonly Float3 normal = ref query.normal;
			ref Float3 shadingNormal = ref query.shading.normal;

			if (Normal == Texture.normal || NormalIntensity.AlmostEquals())
			{
				shadingNormal = normal;
				return;
			}

			Vector128<float> sample = Clamp01(Normal[query.shading.texcoord]);
			Vector128<float> local = Fused(sample, vector2, normalShift);

			//Create transform to move from local direction to world space based
			NormalTransform transform = new NormalTransform(normal);
			Float3 delta = transform.LocalToWorld(ToFloat3(ref local));

			shadingNormal = normal - delta * NormalIntensity;
			shadingNormal = shadingNormal.Normalized;
		}
	}
}