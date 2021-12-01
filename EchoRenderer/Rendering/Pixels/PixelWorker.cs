using System.Runtime.CompilerServices;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics.Intersections;
using EchoRenderer.Rendering.Materials;
using EchoRenderer.Rendering.Memory;
using EchoRenderer.Rendering.Profiles;

namespace EchoRenderer.Rendering.Pixels
{
	public abstract class PixelWorker
	{
		/// <summary>
		/// Returns an object with base type <see cref="Arena"/> which will be passed into the subsequent invocations to <see cref="Render"/>.
		/// </summary>
		public virtual Arena CreateArena(RenderProfile profile, int seed) => new(profile, seed);

		/// <summary>
		/// Invoked before a new rendering process begin on this <see cref="PixelWorker"/>.
		/// Can be used to prepare the worker for future invocations to <see cref="Render"/>.
		/// </summary>
		public virtual void BeforeRender(RenderProfile profile) { }

		/// <summary>
		/// Renders a <see cref="Sample"/> at <paramref name="uv"/>.
		/// </summary>
		/// <param name="uv">
		/// The screen percentage point to work on. X should be normalized and between -0.5 to 0.5;
		/// Y should have the same scale as X and it would depend on the aspect ratio.
		/// </param>
		/// <param name="arena">The <see cref="Arena"/> to use for this sample.</param>
		public abstract Sample Render(Float2 uv, Arena arena);

		/// <summary>
		/// Returns whether <paramref name="query"/> is on an invisible surface and we should just continue through, ignoring this hit
		/// NOTE: this works with 1 IOR white <see cref="Glass"/> materials as well because they are essentially invisible too.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static bool HitPassThrough(in TraceQuery query, in Float3 albedo, in Float3 outgoingWorld) => query.ray.direction == -outgoingWorld && albedo == Float3.one;

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

			public static implicit operator Sample(in Float3 colour) => new(colour);
		}
	}
}