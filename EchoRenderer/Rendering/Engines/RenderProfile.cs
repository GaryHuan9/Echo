using System;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Rendering.Pixels;
using EchoRenderer.Textures.DimensionTwo;

namespace EchoRenderer.Rendering.Engines
{
	/// <summary>
	/// An immutable record that defines the renderer's settings/parameters.
	/// Immutability ensures that the profile never change when all threads are running.
	/// </summary>
	public record RenderProfile
	{
		/// <summary>
		/// The target scene to render.
		/// </summary>
		public PressedScene Scene { get; init; }

		/// <summary>
		/// The fundamental rendering method used for each pixel.
		/// </summary>
		public PixelWorker Method { get; init; }

		/// <summary>
		/// The <see cref="RenderBuffer"/> to render onto.
		/// </summary>
		public RenderBuffer RenderBuffer { get; init; }

		/// <summary>
		/// The maximum number of worker threads concurrently running.
		/// </summary>
		public int WorkerSize { get; init; } = Environment.ProcessorCount;

		/// <summary>
		/// The maximum number of bounce allowed for one sample.
		/// </summary>
		public int BounceLimit { get; init; } = 128;

		/// <summary>
		/// Epsilon lower bound value to determine when an energy is essentially zero.
		/// </summary>
		public Float3 EnergyEpsilon { get; init; } = (Float3)9E-3f;

		public virtual void Validate()
		{
			if (Scene == null) throw ExceptionHelper.Invalid(nameof(Scene), InvalidType.isNull);
			if (Method == null) throw ExceptionHelper.Invalid(nameof(Method), InvalidType.isNull);
			if (RenderBuffer == null) throw ExceptionHelper.Invalid(nameof(RenderBuffer), InvalidType.isNull);

			if (WorkerSize <= 0) throw ExceptionHelper.Invalid(nameof(WorkerSize), WorkerSize, InvalidType.outOfBounds);
			if (BounceLimit < 0) throw ExceptionHelper.Invalid(nameof(BounceLimit), BounceLimit, InvalidType.outOfBounds);
			if (EnergyEpsilon.MinComponent < 0f) throw ExceptionHelper.Invalid(nameof(EnergyEpsilon), EnergyEpsilon, InvalidType.outOfBounds);
		}
	}
}