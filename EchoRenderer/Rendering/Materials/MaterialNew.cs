using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Scattering;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.Materials
{
	public abstract class MaterialNew
	{
		public Texture Albedo { get; set; }
		public Texture Normal { get; set; }

		public float NormalIntensity { get; set; } = 1f;

		static readonly Vector128<float> normalShift = Vector128.Create(-1f, -1f, -2f, 0f);

		/// <summary>
		/// Determines the scattering properties of this material at <paramref name="query"/>.
		/// Initializes a <see cref="BSDF"/>.
		/// </summary>
		public abstract void Scatter(ref HitQuery query, Arena arena);

		/// <summary>
		/// Fills in the shading normal for <paramref name="query"/> based on this <see cref="MaterialNew"/>'s <see cref="Normal"/>.
		/// </summary>
		public unsafe void FillTangentNormal(ref HitQuery query)
		{
			ref readonly Float3 normal = ref query.normal;
			ref Float3 shadingNormal = ref query.shading.normal;

			if (Normal == null || Normal == Texture.normal || NormalIntensity.AlmostEquals())
			{
				shadingNormal = normal;
				return;
			}

			Vector128<float> sample = Normal[query.shading.texcoord];
			Vector128<float> local = Fma.MultiplyAdd(sample, Utilities.vector2, normalShift);

			//Transform local direction to world space based on normal
			Float3 helper = Math.Abs(normal.x) >= 0.9f ? Float3.forward : Float3.right;

			Float3 tangent = Float3.Cross(normal, helper).Normalized;
			Float3 binormal = Float3.Cross(normal, tangent).Normalized;

			float* p = (float*)&local;

			//Transforms direction using 3x3 matrix multiplication
			shadingNormal = normal - new Float3
							(
								tangent.x * p[0] + binormal.x * p[1] + normal.x * p[2],
								tangent.y * p[0] + binormal.y * p[1] + normal.y * p[2],
								tangent.z * p[0] + binormal.z * p[1] + normal.z * p[2]
							) * NormalIntensity;

			shadingNormal = shadingNormal.Normalized;
		}
	}
}