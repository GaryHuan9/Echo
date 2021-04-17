using System;
using System.Runtime.CompilerServices;
using System.Threading;
using CodeHelpers.Mathematics;
using EchoRenderer.Mathematics;
using EchoRenderer.Mathematics.Intersections;

namespace EchoRenderer.Rendering.Pixels
{
	public abstract class PixelWorker
	{
		readonly ThreadLocal<ExtendedRandom> threadRandom = new(() => new ExtendedRandom());
		protected PressedRenderProfile Profile { get; private set; }

		/// <summary>
		/// Returns a thread-safe random number generator that can be used in the invoking thread.
		/// </summary>
		protected ExtendedRandom Random => threadRandom.Value;

		/// <summary>
		/// Returns a thread-safe random value larger than or equals zero, and smaller than one.
		/// </summary>
		protected float RandomValue => Random.NextFloat();

		/// <summary>
		/// Assigns the render profile before a render session begins.
		/// NOTE: This can be used as a "reset" point for the worker.
		/// </summary>
		public virtual void AssignProfile(PressedRenderProfile profile) => Profile = profile;

		/// <summary>
		/// Sample and render at a specific point.
		/// </summary>
		/// <param name="screenUV">The screen percentage point to work on. X should be normalized and between -0.5 to 0.5;
		/// Y should have the same scale as X and it would depend on the aspect ratio.</param>
		public abstract Float3 Render(Float2 screenUV);

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
	}
}