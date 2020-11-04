using System;
using System.Runtime.CompilerServices;
using System.Threading;
using CodeHelpers.Vectors;
using ForceRenderer.Mathematics;
using ForceRenderer.Scenes;

namespace ForceRenderer.Renderers
{
	public abstract class PixelWorker
	{
		protected PixelWorker(RenderProfile profile)
		{
			this.profile = profile;
			scene = profile.scene;
			pressedScene = profile.pressedScene;

			threadRandom = new ThreadLocal<Random>(() => new Random(Thread.CurrentThread.ManagedThreadId ^ Environment.TickCount));
		}

		readonly ThreadLocal<Random> threadRandom;

		protected readonly RenderProfile profile;
		protected readonly Scene scene;
		protected readonly PressedScene pressedScene;

		protected float RandomValue => (float)threadRandom.Value.NextDouble();

		public abstract Float3 Render(Float2 uv);

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		protected bool TryTrace(in Ray ray, out float distance, out int token)
		{
			distance = pressedScene.GetIntersection(ray, out token);
			return distance < float.PositiveInfinity;
		}
	}
}