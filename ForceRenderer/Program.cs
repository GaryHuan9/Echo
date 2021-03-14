using System;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using ForceRenderer.Objects;
using ForceRenderer.Rendering;
using ForceRenderer.Rendering.Pixels;
using ForceRenderer.Rendering.PostProcessing;
using ForceRenderer.Rendering.Tiles;
using ForceRenderer.Terminals;
using ForceRenderer.Textures;

namespace ForceRenderer
{
	public class Program
	{
		static void Main()
		{
			// DenoiserTesting();
			// SimplexNoise();

			// return;

			using Terminal terminal = new Terminal();
			renderTerminal = terminal;

			commandsController = new CommandsController(terminal);
			renderMonitor = new RenderMonitor(terminal);

			terminal.AddSection(commandsController);
			terminal.AddSection(renderMonitor);

			ThreadHelper.MainThread = Thread.CurrentThread;
			RandomHelper.Seed = 47;

			PerformRender();
			Console.ReadKey();
		}

		static RenderEngine renderEngine;
		static Terminal renderTerminal;

		public static CommandsController commandsController;
		public static RenderMonitor renderMonitor;

		static readonly RenderProfile pathTraceFastProfile = new()
															 {
																 Method = new PathTraceWorker(),
																 TilePattern = new CheckerboardPattern(),
																 PixelSample = 24,
																 AdaptiveSample = 180
															 };

		static readonly RenderProfile pathTraceProfile = new()
														 {
															 Method = new PathTraceWorker(),
															 TilePattern = new SpiralPattern(),
															 PixelSample = 32,
															 AdaptiveSample = 400
														 };

		static readonly RenderProfile pathTraceExportProfile = new()
															   {
																   Method = new PathTraceWorker(),
																   TilePattern = new CheckerboardPattern(),
																   PixelSample = 64,
																   AdaptiveSample = 1600
															   };

		static readonly RenderProfile albedoProfile = new()
													  {
														  Method = new AlbedoPixelWorker(),
														  TilePattern = new ScrambledPattern(),
														  PixelSample = 12,
														  AdaptiveSample = 100
													  };

		static readonly RenderProfile bvhQualityProfile = new()
														  {
															  Method = new BVHQualityWorker(),
															  TilePattern = new OrderedPattern(),
															  PixelSample = 1,
															  AdaptiveSample = 0
														  };

		static void PerformRender()
		{
			Int2[] resolutions =
			{
				new(480, 270), new(960, 540), new(1920, 1080),
				new(3840, 2160), new(1024, 1024), new(512, 512)
			};

			Texture2D buffer = new Texture2D(resolutions[1]);
			RenderProfile profile = pathTraceFastProfile;

			profile.Scene = new MaterialBallScene();
			profile.RenderBuffer = buffer;

			using RenderEngine engine = new RenderEngine {Profile = profile};

			renderEngine = engine;
			renderMonitor.Engine = engine;

			commandsController.Log("Assets loaded");

			PerformanceTest setupTest = new PerformanceTest();
			using (setupTest.Start()) engine.Begin();

			commandsController.Log($"Engine Setup Complete: {setupTest.ElapsedMilliseconds}ms");
			engine.WaitForRender();

			buffer.Save("render.fpi");

			using var postProcess = new PostProcessingEngine(buffer);

			postProcess.AddWorker(new BloomWorker(postProcess));
			postProcess.AddWorker(new VignetteWorker(postProcess, 0.24f));
			postProcess.AddWorker(new ColorCorrectionWorker(postProcess, 1f));

			postProcess.Dispatch();
			postProcess.WaitForProcess();

			buffer.Save("render.png");

			//Logs render stats
			double elapsedSeconds = engine.Elapsed.TotalSeconds;
			long completedSample = engine.CompletedSample;

			commandsController.Log($"Completed after {elapsedSeconds:F2} seconds with {completedSample:N0} samples at {completedSample / elapsedSeconds:N0} samples per second.");
			if (profile.Method is BVHQualityWorker qualityWorker) commandsController.Log(qualityWorker.GetQualityText());

			renderEngine = null;
		}

		static void DenoiserTesting()
		{
			Texture2D noisy = Texture2D.Load("render_sponza_noisy.fpi");
			Texture2D albedo = Texture2D.Load("render_sponza_albedo.fpi");

			using var postProcess = new PostProcessingEngine(noisy);

			postProcess.AddWorker(new Denoiser(postProcess, albedo));

			postProcess.Dispatch();
			postProcess.WaitForProcess();

			noisy.Save("render.png");
		}

		static void SimplexNoise()
		{
			Simplex2D simplex = new Simplex2D(new Int2(1920, 1080), 42, 4);
			Texture2D texture = new Texture2D(simplex.size);

			simplex.Tiling = (Float2)1f;
			simplex.Offset = (Float2)1f;

			PerformanceTest test = new PerformanceTest();
			using (test.Start()) simplex.Bake();
			DebugHelper.Log(test.ElapsedMilliseconds);

			texture.CopyFrom(simplex);
			texture.Save("simplex.png");
		}

		[Command]
		static CommandResult Pause()
		{
			if (renderEngine.CurrentState == RenderEngine.State.rendering)
			{
				renderEngine.Pause();
				return new CommandResult("Render pausing...", true);
			}

			return new CommandResult("Cannot pause: not rendering.", false);
		}

		[Command]
		static CommandResult Resume()
		{
			if (renderEngine.CurrentState == RenderEngine.State.paused)
			{
				renderEngine.Resume();
				return new CommandResult("Render resumed.", true);
			}

			return new CommandResult("Cannot resume: not paused.", false);
		}

		[Command]
		static CommandResult Abort()
		{
			if (renderEngine.Rendering)
			{
				renderEngine.Abort();
				return new CommandResult("Render aborted.", true);
			}

			return new CommandResult("Cannot abort: not rendering.", false);
		}

		[Command]
		static CommandResult RewriteTerminal()
		{
			renderTerminal.Rewrite = true;
			return new CommandResult("Terminal rewrote.", true);
		}

		[Command]
		static CommandResult SaveRenderBuffer()
		{
			Texture buffer = renderEngine.Profile.RenderBuffer;

			if (buffer == null) return new CommandResult("No buffer assigned", false);
			if (buffer is not Texture2D texture) return new CommandResult("Unsupported buffer type", false);

			texture.Save("render.png");
			return new CommandResult("Render buffer saved.", true);
		}
	}
}