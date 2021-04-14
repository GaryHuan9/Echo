using System;
using CodeHelpers;
using CodeHelpers.Mathematics;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering.Pixels;
using EchoRenderer.Rendering.Tiles;
using EchoRenderer.Textures;

namespace EchoRenderer.Rendering
{
	public class RenderProfile
	{
		/// <summary>
		/// The target scene to render.
		/// </summary>
		public Scene Scene { get; set; }

		/// <summary>
		/// The texture buffer to render onto.
		/// </summary>
		public Texture RenderBuffer { get; set; }

		/// <summary>
		/// The fundamental rendering method used for each pixel.
		/// </summary>
		public PixelWorker Method { get; set; }

		/// <summary>
		/// The tile pattern used to determine the order of tiles rendered.
		/// </summary>
		public ITilePattern TilePattern { get; set; }

		/// <summary>
		/// The base/minimum number of samples calculated for each pixel.
		/// This first sample pass also determines the sample size for the second pass.
		/// </summary>
		public int PixelSample { get; set; }

		/// <summary>
		/// A multiplier that affects the sample size for the second sample pass.
		/// NOTE: The actual count is determined by the first pass and it can be larger than this value!
		/// </summary>
		public int AdaptiveSample { get; set; }

		/// <summary>
		/// The size of one square tile.
		/// </summary>
		public int TileSize { get; set; } = 32;

		/// <summary>
		/// The number of concurrent tiles being worked on.
		/// </summary>
		public int WorkerSize { get; set; } = Math.Max(1, Environment.ProcessorCount / 2);

		/// <summary>
		/// The maximum number of bounce allowed for one sample.
		/// </summary>
		public int BounceLimit { get; set; } = 64;

		/// <summary>
		/// Epsilon lower bound value to determine when an energy is essentially zero.
		/// </summary>
		public Float3 EnergyEpsilon { get; set; } = (Float3)9E-3f;
	}

	/// <summary>
	/// An immutable structure that is stores a copy of the renderer's settings/profile.
	/// This ensures that the renderer never changes its settings when all threads are running.
	/// </summary>
	public readonly struct PressedRenderProfile
	{
		public PressedRenderProfile(RenderProfile profile)
		{
			scene = new PressedScene(profile.Scene);
			renderBuffer = profile.RenderBuffer;

			worker = profile.Method;
			tilePattern = profile.TilePattern;

			if (renderBuffer == null) throw ExceptionHelper.Invalid(nameof(renderBuffer), InvalidType.isNull);
			if (worker == null) throw ExceptionHelper.Invalid(nameof(worker), InvalidType.isNull);
			if (tilePattern == null) throw ExceptionHelper.Invalid(nameof(tilePattern), InvalidType.isNull);

			pixelSample = profile.PixelSample;
			adaptiveSample = profile.AdaptiveSample;

			if (pixelSample <= 0) throw ExceptionHelper.Invalid(nameof(pixelSample), pixelSample, InvalidType.outOfBounds);
			if (adaptiveSample < 0) throw ExceptionHelper.Invalid(nameof(adaptiveSample), adaptiveSample, InvalidType.outOfBounds);

			tileSize = profile.TileSize;
			workerSize = profile.WorkerSize;

			if (tileSize <= 0) throw ExceptionHelper.Invalid(nameof(tileSize), tileSize, InvalidType.outOfBounds);
			if (workerSize <= 0) throw ExceptionHelper.Invalid(nameof(workerSize), workerSize, InvalidType.outOfBounds);

			bounceLimit = profile.BounceLimit;
			energyEpsilon = profile.EnergyEpsilon;

			if (bounceLimit < 0) throw ExceptionHelper.Invalid(nameof(bounceLimit), bounceLimit, InvalidType.outOfBounds);
			if (energyEpsilon.MinComponent < 0f) throw ExceptionHelper.Invalid(nameof(energyEpsilon), energyEpsilon, InvalidType.outOfBounds);
		}

		public readonly PressedScene scene;
		public readonly Texture renderBuffer;

		public readonly PixelWorker worker;
		public readonly ITilePattern tilePattern;

		public readonly int pixelSample;
		public readonly int adaptiveSample;

		public readonly int tileSize;
		public readonly int workerSize;

		public readonly int bounceLimit;
		public readonly Float3 energyEpsilon;
	}
}