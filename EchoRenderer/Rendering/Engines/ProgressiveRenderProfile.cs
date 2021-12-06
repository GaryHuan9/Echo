using CodeHelpers;
using EchoRenderer.Rendering.Profiles;

namespace EchoRenderer.Rendering.Engines
{
	public record ProgressiveRenderProfile : RenderProfile
	{
		/// <summary>
		/// The number of samples each pixel will receive until the worker moves onto the next pixel.
		/// After the entire resolution has been worked on for this many samples, the worker will
		/// return back to this pixel and resume its progress for another indicated number of samples.
		/// </summary>
		public int EpochSample { get; init; } = 3;

		/// <summary>
		/// The base/minimum number of regular epochs we render before turning on adaptive sampling.
		/// Epochs after this value will use the deviation of each pixel to focus on more important places.
		/// </summary>
		public int EpochLength { get; init; } = 14;

		/// <summary>
		/// The number of additional samples used for each pixel during each epoch when adaptive sampling is on.
		/// NOTE: This number is added with <see cref="EpochSample"/> then multiplied to the pixel's deviation.
		/// </summary>
		public int AdaptiveSample { get; init; }

		public override int BaseSample => EpochSample;

		public override void Validate()
		{
			base.Validate();

			if (EpochSample <= 0) throw ExceptionHelper.Invalid(nameof(EpochSample), EpochSample, InvalidType.outOfBounds);
			if (EpochLength <= 0) throw ExceptionHelper.Invalid(nameof(EpochLength), EpochLength, InvalidType.outOfBounds);
			if (AdaptiveSample < 0) throw ExceptionHelper.Invalid(nameof(AdaptiveSample), AdaptiveSample, InvalidType.outOfBounds);
		}
	}
}