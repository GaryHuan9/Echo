using System;
using System.Runtime.CompilerServices;
using System.Threading;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;

namespace ForceRenderer.Renderers
{
	public abstract class PixelWorker
	{
		protected PixelWorker(RenderEngine.Profile profile)
		{
			this.profile = profile;
			threadRandom = new ThreadLocal<ExtendedRandom>(() => new ExtendedRandom());
		}

		readonly ThreadLocal<ExtendedRandom> threadRandom;
		protected readonly RenderEngine.Profile profile;

		long _intersectionPerformed;

		public long IntersectionPerformed => Interlocked.Read(ref _intersectionPerformed);

		/// <summary>
		/// Returns a thread-safe random number generator that can be used in the invoking thread.
		/// </summary>
		protected ExtendedRandom Random => threadRandom.Value;

		/// <summary>
		/// Returns a thread-safe random value larger than or equals zero, and smaller than one.
		/// </summary>
		protected float RandomValue => Random.NextFloat();

		/// <summary>
		/// Sample and render at a specific point.
		/// </summary>
		/// <param name="screenUV">The screen percentage point to work on. X should be normalized and between -0.5 to 0.5;
		/// Y should have the same scale as X and it would depend on the aspect ratio.</param>
		public abstract Float3 Render(Float2 screenUV);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool GetIntersection(in Ray ray, out Hit hit)
		{
			hit = profile.pressed.bvh.GetIntersection(ray);
			Interlocked.Increment(ref _intersectionPerformed);

			return float.IsFinite(hit.distance);
		}
	}
}