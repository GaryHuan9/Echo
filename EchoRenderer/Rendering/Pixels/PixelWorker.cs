using System;
using System.Runtime.CompilerServices;
using System.Threading;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Engines;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.PostProcessing;

namespace EchoRenderer.Rendering.Pixels
{
	public abstract class PixelWorker
	{
		protected RenderProfile Profile { get; private set; }

		/// <summary>
		/// Assigns the render profile before a render session begins.
		/// NOTE: This can be used as a "reset" point for the worker.
		/// </summary>
		public virtual void AssignProfile(RenderProfile profile) => Profile = profile;

		/// <summary>
		/// Renders a <see cref="Sample"/> at <paramref name="screenUV"/>.
		/// </summary>
		/// <param name="screenUV">
		/// The screen percentage point to work on. X should be normalized and between -0.5 to 0.5;
		/// Y should have the same scale as X and it would depend on the aspect ratio.
		/// </param>
		/// <param name="random">The RNG to use. Should be unique to each thread.</param>
		public abstract Sample Render(Float2 screenUV, ExtendedRandom random);

		/// <summary>
		/// Creates a new ray with <paramref name="direction"/> and an origin that is slightly shifted according to
		/// <paramref name="hit"/> to avoid self intersecting with the previous geometries.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static Ray CreateBiasedRay(in Float3 direction, in CalculatedHit hit)
		{
			const float BiasScale = 2.4E-4f; //Ray origins will shift to try to get this far from the geometry
			const float MaxLength = 5.7E-4f; //The maximum distance origins are allowed to move before stopping

			float distance = direction.Dot(hit.normalRaw);
			if (distance < 0f) distance = -distance;

			distance = Math.Min(BiasScale / distance, MaxLength);
			return new Ray(hit.position + direction * distance, direction);
		}

		/// <summary>
		/// Returns whether <paramref name="hit"/> is on an invisible surface and we should just continue through, ignoring this hit
		/// NOTE: this works with 1 IOR white <see cref="Glass"/> materials as well because they are essentially invisible too.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static bool HitPassThrough(in CalculatedHit hit, in Float3 albedo, in Float3 direction) => hit.direction == direction && albedo == Float3.one;

		public readonly struct Sample
		{
			public Sample(in Float3 colour, in Float3 albedo = default, in Float3 normal = default, float zDepth = default)
			{
				this.colour = colour;
				this.albedo = albedo;
				this.normal = normal;
				this.zDepth = zDepth;
			}

			public readonly Float3 colour; //We use the British spelling here so that all the names line up (sort of)
			public readonly Float3 albedo;
			public readonly Float3 normal;
			public readonly float zDepth;

			public bool IsNaN => float.IsNaN(colour.x) || float.IsNaN(colour.y) || float.IsNaN(colour.z) ||
								 float.IsNaN(albedo.x) || float.IsNaN(albedo.y) || float.IsNaN(albedo.z) ||
								 float.IsNaN(normal.x) || float.IsNaN(normal.y) || float.IsNaN(normal.z) ||
								 float.IsNaN(zDepth);

			public static implicit operator Sample(in Float3 colour) => new Sample(colour);
		}
	}
}