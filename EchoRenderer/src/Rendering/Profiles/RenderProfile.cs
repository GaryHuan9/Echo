﻿using System;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Objects.Preparation;
using EchoRenderer.Rendering.Pixels;
using EchoRenderer.Textures.Grid;

namespace EchoRenderer.Rendering.Profiles
{
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
		public PixelWorker Method { get; init; }

		/// <summary>
		/// The destination <see cref="RenderBuffer"/> to render onto.
		/// </summary>
		public RenderBuffer RenderBuffer { get; init; }

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

		/// <summary>
		/// The maximum number of worker threads concurrently running.
		/// </summary>
		public int WorkerSize { get; init; } = Environment.ProcessorCount;

		/// <summary>
		/// The maximum number of bounce allowed for one sample.
		/// </summary>
		public int BounceLimit { get; init; } = 128;

		/// <summary>
		/// Epsilon lower bound value to determine when a radiance is essentially zero.
		/// </summary>
		public Float3 RadianceEpsilon { get; init; } = Utilities.ToFloat3(Utilities.CreateLuminance(12E-3f));

		/// <summary>
		/// Returns whether <paramref name="radiance"/> is considered as empty or zero
		/// based on the <see cref="RadianceEpsilon"/> of this <see cref="RenderProfile"/>.
		/// </summary>
		public bool IsZero(in Float3 radiance) => radiance <= RadianceEpsilon;

		public virtual void Validate()
		{
			if (Scene == null) throw ExceptionHelper.Invalid(nameof(Scene), InvalidType.isNull);
			if (Method == null) throw ExceptionHelper.Invalid(nameof(Method), InvalidType.isNull);
			if (RenderBuffer == null) throw ExceptionHelper.Invalid(nameof(RenderBuffer), InvalidType.isNull);

			if (PixelSample <= 0) throw ExceptionHelper.Invalid(nameof(PixelSample), PixelSample, InvalidType.outOfBounds);
			if (AdaptiveSample < 0) throw ExceptionHelper.Invalid(nameof(AdaptiveSample), AdaptiveSample, InvalidType.outOfBounds);

			if (WorkerSize <= 0) throw ExceptionHelper.Invalid(nameof(WorkerSize), WorkerSize, InvalidType.outOfBounds);
			if (BounceLimit < 0) throw ExceptionHelper.Invalid(nameof(BounceLimit), BounceLimit, InvalidType.outOfBounds);
			if (RadianceEpsilon.MinComponent < 0f) throw ExceptionHelper.Invalid(nameof(RadianceEpsilon), RadianceEpsilon, InvalidType.outOfBounds);
		}
	}
}