using System.Threading;
using CodeHelpers.Mathematics;
using ForceRenderer.Mathematics;

namespace ForceRenderer.Rendering.Pixels
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
	}
}