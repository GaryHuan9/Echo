using System;
using System.Runtime.Intrinsics;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using EchoRenderer.IO;
using EchoRenderer.Mathematics.Accelerators;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering;
using EchoRenderer.Rendering.Engines;
using EchoRenderer.Rendering.Pixels;
using EchoRenderer.Rendering.PostProcessing;
using EchoRenderer.Rendering.PostProcessing.ToneMappers;
using EchoRenderer.Rendering.Profiles;
using EchoRenderer.Terminals;
using EchoRenderer.Textures;
using EchoRenderer.Textures.DimensionTwo;

namespace EchoRenderer
{
	public class Program
	{
		static void Main()
		{
			// SimplexNoise();
			// FontTesting();
			// PostProcessTesting();

			// return;

			using Terminal terminal = renderTerminal = new Terminal();

			terminal.AddSection(new CommandsController(terminal));
			terminal.AddSection(renderMonitor = new RenderMonitor(terminal));

			ThreadHelper.MainThread = Thread.CurrentThread;
			RandomHelper.Seed = 47;

			PerformRender();
			Console.ReadKey();
		}

		static TiledRenderEngine renderEngine;
		static Terminal renderTerminal;
		static RenderMonitor renderMonitor;

		static readonly TiledRenderProfile pathTraceFastProfile = new()
																  {
																	  Method = new DirectLightingWorker(),
																	  TilePattern = new CheckerboardPattern(),
																	  PixelSample = 16,
																	  AdaptiveSample = 80
																  };

		static readonly TiledRenderProfile pathTraceProfile = new()
															  {
																  Method = new PathTraceWorker(),
																  TilePattern = new CheckerboardPattern(),
																  PixelSample = 40,
																  AdaptiveSample = 400
															  };

		static readonly TiledRenderProfile pathTraceExportProfile = new()
																	{
																		Method = new PathTraceWorker(),
																		TilePattern = new CheckerboardPattern(),
																		PixelSample = 64,
																		AdaptiveSample = 1600
																	};

		static readonly TiledRenderProfile albedoProfile = new()
														   {
															   Method = new AlbedoPixelWorker(),
															   TilePattern = new ScrambledPattern(),
															   PixelSample = 12,
															   AdaptiveSample = 80
														   };

		static readonly TiledRenderProfile acceleratorQualityProfile = new()
																	   {
																		   Method = new AcceleratorQualityWorker(),
																		   TilePattern = new OrderedPattern(),
																		   PixelSample = 1,
																		   AdaptiveSample = 0
																	   };

		static readonly ScenePressProfile scenePressProfile = new()
															  {
																  AcceleratorProfile = new TraceAcceleratorProfile
																					   {
																						   AcceleratorType = typeof(QuadBoundingVolumeHierarchy)
																					   }
															  };

		static void PerformRender()
		{
			Int2[] resolutions = //Different resolutions for easy selection
			{
				new(480, 270), new(960, 540), new(1920, 1080),
				new(3840, 2160), new(1024, 1024), new(512, 512)
			};

			RenderBuffer buffer = new RenderBuffer(resolutions[1]);  //Selects resolution and create buffer
			TiledRenderProfile renderProfile = pathTraceFastProfile; //Selects or creates render profile
			Scene scene = new SingleBunny();                         //Selects or creates scene

			DebugHelper.Log("Assets loaded");

			PerformanceTest setupTest = new PerformanceTest();
			using TiledRenderEngine engine = new TiledRenderEngine();

			renderEngine = engine;
			renderMonitor.Engine = engine;

			using (setupTest.Start())
			{
				var pressed = new PressedScene(scene, scenePressProfile);

				renderProfile = renderProfile with
								{
									RenderBuffer = buffer,
									Scene = pressed
								};

				engine.Begin(renderProfile); //Initializes render
			}

			DebugHelper.Log($"Engine Setup Complete: {setupTest.ElapsedMilliseconds}ms");
			engine.WaitForRender(); //Main thread wait for engine to complete render

			buffer.Save("render.fpi"); //Save floating point image before post processing

			using var postProcess = new PostProcessingEngine(buffer);

			if (renderProfile.Method is AcceleratorQualityWorker) //Creates different post processing workers based on render method
			{
				postProcess.AddWorker(new AcceleratorQualityVisualizer(postProcess)); //Only used for accelerator quality testing
			}
			else if (false) //Enable or disable post processing
			{
				if (renderProfile.Method is PathTraceWorker)
				{
					// postProcess.AddWorker(new DenoiseOidn(postProcess));
				}

				//Standard render post processing layers
				postProcess.AddWorker(new Bloom(postProcess));
				postProcess.AddWorker(new BasicShoulder(postProcess));
				// postProcess.AddWorker(new DepthOfField(postProcess));
				postProcess.AddWorker(new Vignette(postProcess));
				postProcess.AddWorker(new Watermark(postProcess)); //Disable this if do not want watermark
			}

			postProcess.AddWorker(new OutputBarrier(postProcess));

			postProcess.Dispatch();
			postProcess.WaitForProcess(); //Wait for post processing to finish

			buffer.Save("render.png"); //Save final image

			//Logs render stats
			double elapsedSeconds = engine.Elapsed.TotalSeconds;
			long completedSample = engine.CompletedSample;

			DebugHelper.Log($"Completed after {elapsedSeconds:F2} seconds with {completedSample:N0} samples at {completedSample / elapsedSeconds:N0} samples per second.");
			renderEngine = null;
		}

		static void SimplexNoise()
		{
			TestGenerative simplex = new TestGenerative(42, 4);
			Array2D texture = new Array2D((Int2)1080);

			simplex.Tiling = (Float2)1f;
			simplex.Offset = (Float2)1f;

			texture.CopyFrom(simplex);
			texture.Save("simplex.png");
		}

		static void FontTesting()
		{
			Font font = Font.Find("Assets/Fonts/JetBrainsMono/FontMap.png");
			Array2D output = new Array2D((Int2)2048);

			foreach (Int2 position in output.size.Loop()) output[position] = Vector128.Create(0f, 0f, 1f, 1f);
			font.Draw(output, "The quick fox does stuff", (Float2)1024f, new Font.Style(100f, Float4.one));

			output.Save("render.png");
		}

		static void PostProcessTesting()
		{
			Array2D texture = Texture2D.Load("render.fpi");
			RenderBuffer buffer = new RenderBuffer(texture.size);

			buffer.CopyFrom(texture);

			using PostProcessingEngine engine = new PostProcessingEngine(buffer);

			//NOTE: Oidn do not have normal and albedo data here, so its quality might be pretty bad
			// engine.AddWorker(new DenoiseOidn(engine));

			engine.AddWorker(new Bloom(engine));
			engine.AddWorker(new Reinhard(engine));
			engine.AddWorker(new Vignette(engine));
			engine.AddWorker(new OutputBarrier(engine));

			engine.Dispatch();
			engine.WaitForProcess();

			buffer.Save("post process.png");
		}

		[Command]
		static CommandResult Pause()
		{
			if (renderEngine.CurrentState == TiledRenderEngine.State.rendering)
			{
				renderEngine.Pause();
				return new CommandResult("Render pausing...", true);
			}

			return new CommandResult("Cannot pause: not rendering.", false);
		}

		[Command]
		static CommandResult Resume()
		{
			if (renderEngine.CurrentState == TiledRenderEngine.State.paused)
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
			RenderBuffer buffer = renderEngine.CurrentProfile.RenderBuffer;
			if (buffer == null) return new CommandResult("No buffer assigned", false);

			buffer.Save("render.png");
			return new CommandResult("Render buffer saved.", true);
		}
	}
}