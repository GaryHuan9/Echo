using CodeHelpers;

namespace EchoRenderer.Rendering.Engines
{
	public record ProgressiveRenderProfile : RenderProfile
	{
		/// <summary>
		/// The number of samples each pixel will receive until the worker moves onto the next pixel.
		/// After the entire resolution has been worked on for this many samples, the worker will
		/// return back to this pixel and resume its progress for another indicated number of samples.
		/// </summary>
		public int EpochSample { get; init; }

		public override void Validate()
		{
			base.Validate();

			if (EpochSample <= 0) throw ExceptionHelper.Invalid(nameof(EpochSample), EpochSample, InvalidType.outOfBounds);
		}
	}
}