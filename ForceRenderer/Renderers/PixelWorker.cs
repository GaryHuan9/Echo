using System;
using System.Runtime.CompilerServices;
using System.Threading;
using CodeHelpers.Vectors;
using ForceRenderer.Mathematics;

namespace ForceRenderer.Renderers
{
	public abstract class PixelWorker
	{
		protected PixelWorker(RenderEngine.Profile profile)
		{
			this.profile = profile;
			threadRandom = new ThreadLocal<Random>(() => new Random(Thread.CurrentThread.ManagedThreadId ^ Environment.TickCount));
		}

		readonly ThreadLocal<Random> threadRandom;
		protected readonly RenderEngine.Profile profile;

		protected float RandomValue => (float)threadRandom.Value.NextDouble();

		/// <summary>
		/// Sample and render at a specific point.
		/// </summary>
		/// <param name="uv">The screen percentage point to work on. X should be normalized and between -0.5 to 0.5;
		/// Y should have the same scale as X and it would depend on the aspect ratio.</param>
		public abstract Float3 Render(Float2 uv);

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		protected bool TryTrace(in Ray ray, out float distance, out int token)
		{
			distance = profile.pressed.GetIntersection(ray, out token);
			return distance < float.PositiveInfinity;
		}
	}
}