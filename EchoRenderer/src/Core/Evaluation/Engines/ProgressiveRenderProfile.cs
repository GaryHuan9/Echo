using CodeHelpers;

namespace EchoRenderer.Core.Evaluation.Engines;

public record ProgressiveRenderProfile : RenderProfile
{
	/// <summary>
	/// The number of samples each pixel will receive until the worker moves onto the next pixel.
	/// After the entire resolution has been worked on for this many samples, the worker will
	/// return back to this pixel and resume its progress for another indicated number of samples.
	/// NOTE: epoch sampling is first used when rendering progressively, then we switch to the regular pixel + adaptive sampling technique
	/// </summary>
	public int EpochSample { get; init; } = 3;

	/// <summary>
	/// The base/minimum number of regular epochs we render before turning on adaptive sampling.
	/// Epochs after this value will use the deviation of each pixel to focus on more important places.
	/// </summary>
	public int EpochLength { get; init; } = 14;

	public override void Validate()
	{
		base.Validate();

		if (EpochSample <= 0) throw ExceptionHelper.Invalid(nameof(EpochSample), EpochSample, InvalidType.outOfBounds);
		if (EpochLength <= 0) throw ExceptionHelper.Invalid(nameof(EpochLength), EpochLength, InvalidType.outOfBounds);
	}
}