using System;
using CodeHelpers;
using Echo.Common;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Textures.Grid;

namespace Echo.Core.Evaluation;

/// <summary>
/// An immutable record that defines the renderer's settings/parameters.
/// Immutability ensures that the profile never change when all threads are running.
/// </summary>
public abstract record RenderProfile : IProfile
{
	/// <summary>
	/// The target scene to render.
	/// </summary>
	public PreparedScene Scene { get; init; }

	/// <summary>
	/// The fundamental rendering method used for each pixel.
	/// </summary>
	public Evaluator Method { get; init; }

	/// <summary>
	/// The destination <see cref="RenderBuffer"/> to render onto.
	/// </summary>
	public RenderBuffer RenderBuffer { get; init; }

	/// <summary>
	/// The maximum number of worker threads concurrently running.
	/// </summary>
	public int WorkerSize { get; init; } = Environment.ProcessorCount;

	/// <summary>
	/// The minimum (i.e. base) number of consecutive samples performed on each pixel.
	/// </summary>
	public int PixelSample { get; init; }

	/// <summary>
	/// The number of additional samples that can be performed on each pixel if its variance is still higher than the desired amount.
	/// </summary>
	public int AdaptiveSample { get; init; }

	/// <summary>
	/// The maximum number of consecutive samples that can be done on one pixel.
	/// </summary>
	public int TotalSample => PixelSample + AdaptiveSample;

	/// <inheritdoc/>
	public virtual void Validate()
	{
		if (Scene == null) throw ExceptionHelper.Invalid(nameof(Scene), InvalidType.isNull);
		if (Method == null) throw ExceptionHelper.Invalid(nameof(Method), InvalidType.isNull);
		if (RenderBuffer == null) throw ExceptionHelper.Invalid(nameof(RenderBuffer), InvalidType.isNull);

		if (WorkerSize <= 0) throw ExceptionHelper.Invalid(nameof(WorkerSize), WorkerSize, InvalidType.outOfBounds);
		if (PixelSample <= 0) throw ExceptionHelper.Invalid(nameof(PixelSample), PixelSample, InvalidType.outOfBounds);
		if (AdaptiveSample < 0) throw ExceptionHelper.Invalid(nameof(AdaptiveSample), AdaptiveSample, InvalidType.outOfBounds);
	}
}