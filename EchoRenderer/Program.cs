using System;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.IO;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering;
using EchoRenderer.Rendering.Pixels;
using EchoRenderer.Rendering.PostProcessing;
using EchoRenderer.Rendering.Tiles;
using EchoRenderer.Terminals;
using EchoRenderer.Textures;

namespace EchoRenderer
{
	public class Program
	{
		static void Main()
		{
			// SimplexNoise();
			// FontTesting();

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
																 PixelSample = 16,
																 AdaptiveSample = 80
															 };

		static readonly RenderProfile pathTraceProfile = new()
														 {
															 Method = new PathTraceWorker(),
															 TilePattern = new CheckerboardPattern(),
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
														  AdaptiveSample = 80
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
			Int2[] resolutions = //Different resolutions for easy selection
			{
				new(480, 270), new(960, 540), new(1920, 1080),
				new(3840, 2160), new(1024, 1024), new(512, 512)
			};

			RenderBuffer buffer = new RenderBuffer(resolutions[1]); //Selects resolution and create buffer
			RenderProfile profile = pathTraceFastProfile;           //Selects or creates render profile

			profile.Scene = new LightedBMWScene(); //Creates/loads scene to render
			profile.RenderBuffer = buffer;

			using RenderEngine engine = new RenderEngine {Profile = profile};

			renderEngine = engine;
			renderMonitor.Engine = engine;

			commandsController.Log("Assets loaded");

			PerformanceTest setupTest = new PerformanceTest();
			using (setupTest.Start()) engine.Begin(); //Initializes render

			commandsController.Log($"Engine Setup Complete: {setupTest.ElapsedMilliseconds}ms");
			engine.WaitForRender(); //Main thread wait for engine to complete render

			buffer.Save("render.fpi"); //Save floating point image before post processing

			using var postProcess = new PostProcessingEngine(buffer);

			if (profile.Method is BVHQualityWorker) //Creates different post processing workers based on render method
			{
				postProcess.AddWorker(new BVHQualityVisualizer(postProcess)); //Only used for BVH quality testing
			}
			else
			{
				if (profile.Method is PathTraceWorker)
				{
					postProcess.AddWorker(new DenoiseOidn(postProcess));
				}

				//Standard render post processing layers
				postProcess.AddWorker(new Bloom(postProcess));
				postProcess.AddWorker(new ToneMapping(postProcess, 1f));
				postProcess.AddWorker(new Watermark(postProcess)); //Disable this if do not want watermark
				postProcess.AddWorker(new Vignette(postProcess, 0.18f));
			}

			postProcess.AddWorker(new OutputBarrier(postProcess));

			postProcess.Dispatch();
			postProcess.WaitForProcess(); //Wait for post processing to finish

			buffer.Save("render.png"); //Save final image

			//Logs render stats
			double elapsedSeconds = engine.Elapsed.TotalSeconds;
			long completedSample = engine.CompletedSample;

			commandsController.Log($"Completed after {elapsedSeconds:F2} seconds with {completedSample:N0} samples at {completedSample / elapsedSeconds:N0} samples per second.");
			renderEngine = null;
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

		static void FontTesting()
		{
			Font font = new Font("Assets/Fonts/JetbrainsMono/FontMap.png");
			Texture2D output = new Texture2D(new Int2(2048, 2048));

			foreach (Int2 position in output.size.Loop()) output[position] = new Float4(0f, 0f, 1f, 1f);

			font.Draw(output, "Hello Abe how are you", new Font.Style(new Float2(1024f, 1024f), 100f, Float4.one));

			output.Save("render.png");
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
			RenderBuffer buffer = renderEngine.Profile.RenderBuffer;
			if (buffer == null) return new CommandResult("No buffer assigned", false);

			buffer.Save("render.png");
			return new CommandResult("Render buffer saved.", true);
		}
	}
}