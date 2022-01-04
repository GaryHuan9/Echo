using System.Runtime.Intrinsics;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Primitives;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering.Materials
{
	using static Utilities;

	public abstract class Material
	{
		public Texture Albedo { get; set; }
		public Texture Normal { get; set; }

		public float NormalIntensity { get; set; } = 1f;

		static readonly Vector128<float> normalShift = Vector128.Create(-1f, -1f, -2f, 0f);

		/// <summary>
		/// Invoked before a new render session begins; can be used to execute
		/// any kind of preprocessing work for this <see cref="Material"/>.
		/// </summary>
		public virtual void Prepare()
		{
			Albedo ??= Texture.black;
			Normal ??= Texture.normal;
		}

		/// <summary>
		/// Determines the scattering properties of this material at <paramref name="interaction"/>
		/// and potentially initializes the appropriate properties in <paramref name="interaction"/>.
		/// </summary>
		public abstract void Scatter(ref Interaction interaction, Arena arena);

		/// <summary>
		/// Applies this <see cref="Material"/>'s <see cref="Normal"/>
		/// mapping at <paramref name="texcoord"/> to <paramref name="normal"/>.
		/// </summary>
		public void ApplyNormalMapping(in Float2 texcoord, ref Float3 normal)
		{
			if (Normal == Texture.normal || NormalIntensity.AlmostEquals()) return;

			Vector128<float> sample = Clamp01(Normal[texcoord]);
			Vector128<float> local = Fused(sample, vector2, normalShift);

			//Create transform to move from local direction to world space based
			NormalTransform transform = new NormalTransform(normal);
			Float3 delta = transform.LocalToWorld(ToFloat3(ref local));

			normal -= delta * NormalIntensity;
			normal = normal.Normalized;
		}

		/// <summary>
		/// Samples <paramref name="texture"/> at <paramref name="interaction"/> as a <see cref="Float4"/>.
		/// </summary>
		protected Float4 Sample(Texture texture, in Interaction interaction) => ToFloat4(texture[interaction.texcoord]);
	}
}