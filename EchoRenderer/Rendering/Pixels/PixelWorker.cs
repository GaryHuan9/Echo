using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Engines;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Profiles;

namespace EchoRenderer.Rendering.Pixels
{
	public abstract class PixelWorker
	{
		protected RenderProfile Profile { get; private set; }

		/// <summary>
		/// Should create and return a new object of base type <see cref="Arena"/> with <paramref name="hash"/>.
		/// NOTE: The returned <see cref="Arena"/> will be exactly the allocator used for <see cref="Render"/>.
		/// </summary>
		public abstract Arena CreateArena(int hash);

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
		/// <param name="arena">The <see cref="Arena"/> to use for this sample.</param>
		public abstract Sample Render(Float2 screenUV, Arena arena);

		/// <summary>
		/// Returns whether <paramref name="query"/> is on an invisible surface and we should just continue through, ignoring this hit
		/// NOTE: this works with 1 IOR white <see cref="Glass"/> materials as well because they are essentially invisible too.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static bool HitPassThrough(in HitQuery query, in Float3 albedo, in Float3 direction) => query.ray.direction == direction && albedo == Float3.one;

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