﻿using System;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Packed;
using CodeHelpers.Threads;
using EchoRenderer.Core.Evaluation.Engines;
using EchoRenderer.Core.Evaluation.Evaluators;
using EchoRenderer.Core.PostProcess;
using EchoRenderer.Core.PostProcess.ToneMappers;
using EchoRenderer.Core.Scenic;
using EchoRenderer.Core.Scenic.Examples;
using EchoRenderer.Core.Scenic.Preparation;
using EchoRenderer.Core.Textures.Colors;
using EchoRenderer.Core.Textures.Generative;
using EchoRenderer.Core.Textures.Grid;
using EchoRenderer.InOut;
using EchoRenderer.Terminals;

namespace EchoRenderer;

public class Program
{
	static void Main()
	{
		// SimplexNoise();
		// FontTesting();
		// PostProcessTesting();
		// CompareImages();

		// return;

		using Terminal terminal = renderTerminal = new Terminal();

		terminal.AddSection(new CommandsController(terminal));
		terminal.AddSection(renderMonitor = new RenderMonitor(terminal));

		ThreadHelper.MainThread = Thread.CurrentThread;
		RandomHelper.Seed = 47;

#if DEBUG
		DebugHelper.LogWarning("Performing render in DEBUG mode");
#endif

		PerformRender();
		Console.ReadKey();
	}

	static TiledRenderEngine renderEngine;
	static Terminal renderTerminal;
	static RenderMonitor renderMonitor;

	static readonly TiledRenderProfile pathTraceFastProfile = new()
	{
		Method = new PathTracedEvaluator(),
		TilePattern = new CheckerboardPattern(),
		PixelSample = 16,
		AdaptiveSample = 80
	};

	static readonly TiledRenderProfile pathTraceProfile = new()
	{
		Method = new PathTracedEvaluator(),
		TilePattern = new CheckerboardPattern(),
		PixelSample = 40,
		AdaptiveSample = 400
	};

	static readonly TiledRenderProfile pathTraceExportProfile = new()
	{
		Method = new PathTracedEvaluator(),
		TilePattern = new CheckerboardPattern(),
		PixelSample = 64,
		AdaptiveSample = 1600
	};

	static readonly TiledRenderProfile albedoProfile = new()
	{
		Method = new AlbedoEvaluator(),
		TilePattern = new ScrambledPattern(),
		PixelSample = 12,
		AdaptiveSample = 80
	};

	static readonly TiledRenderProfile aggregatorQualityProfile = new()
	{
		Method = new AggregatorQualityEvaluator(),
		TilePattern = new OrderedPattern(),
		PixelSample = 1,
		AdaptiveSample = 0
	};

	static readonly ScenePrepareProfile scenePrepareProfile = new();

	static void PerformRender()
	{
		Int2[] resolutions = //Different resolutions for easy selection
		{
			new(480, 270), new(960, 540), new(1920, 1080),
			new(3840, 2160), new(1024, 1024), new(512, 512)
		};

		RenderBuffer buffer = new RenderBuffer(resolutions[1]); //Selects resolution and create buffer
		Scene scene = new SingleBunny();                        //Selects and creates scene

		var renderProfile = new TiledRenderProfile
		{
			Method = new BruteForcedEvaluator(),
			TilePattern = new CheckerboardPattern(),
			// WorkerSize = 1,
			PixelSample = 64,
			//AdaptiveSample = 400
		};

		DebugHelper.Log("Assets loaded");

		PerformanceTest setupTest = new PerformanceTest();
		using TiledRenderEngine engine = new TiledRenderEngine();

		renderEngine = engine;
		renderMonitor.Engine = engine;

		using (setupTest.Start())
		{
			var prepared = new PreparedScene(scene, scenePrepareProfile);

			renderProfile = renderProfile with
			{
				RenderBuffer = buffer,
				Scene = prepared
			};

			engine.Begin(renderProfile); //Initializes render
		}

		DebugHelper.Log($"Engine Setup Complete: {setupTest.ElapsedMilliseconds}ms");
		engine.WaitForRender(); //Main thread wait for engine to complete render

		buffer.Save("render.fpi"); //Save floating point image before post processing

		using var postProcess = new PostProcessingEngine(buffer);

		if (renderProfile.Method is AggregatorQualityEvaluator) //Creates different post processing workers based on render method
		{
			postProcess.AddWorker(new AggregatorQualityVisualizer(postProcess)); //Only used for aggregator quality testing
		}
		else if (false) //Enable or disable post processing
		{
			if (renderProfile.Method is PathTracedEvaluator)
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

		postProcess.Dispatch();
		postProcess.WaitForProcess(); //Wait for post processing to finish

		buffer.Save("render.png"); //Save final image

		//Logs render stats
		double elapsedSeconds = engine.Elapsed.TotalSeconds;
		long completedSample = engine.CompletedSample;

		DebugHelper.Log($"Completed after {elapsedSeconds:F2} seconds with {completedSample:N0} samples at {completedSample / elapsedSeconds:N0} samples per second.");
	}

	static void SimplexNoise()
	{
		var simplex = new TestGenerative(42, 4);
		var texture = new ArrayGrid<RGB128>((Int2)1080);

		simplex.Tiling = (Float2)1f;
		simplex.Offset = (Float2)1f;

		texture.CopyFrom(simplex);
		texture.Save("simplex.png");
	}

	static void FontTesting()
	{
		var font = Font.Find("Assets/Fonts/JetBrainsMono/FontMap.png");
		var output = new ArrayGrid<RGB128>((Int2)2048);

		output.ForEach(position => output[position] = new RGB128(0f, 0f, 1f));
		font.Draw(output, "The quick fox does stuff", (Float2)1024f, new Font.Style(100f));

		output.Save("render.png");
	}

	static void PostProcessTesting()
	{
		var texture = TextureGrid<RGB128>.Load("render.fpi");
		RenderBuffer buffer = new RenderBuffer(texture.size);

		buffer.CopyFrom(texture);

		using PostProcessingEngine engine = new PostProcessingEngine(buffer);

		//NOTE: Oidn does not have normal and albedo data here, so its quality might be pretty bad
		// engine.AddWorker(new DenoiseOidn(engine));

		engine.AddWorker(new Bloom(engine));
		engine.AddWorker(new Reinhard(engine));
		engine.AddWorker(new Vignette(engine));

		engine.Dispatch();
		engine.WaitForProcess();

		buffer.Save("post process.png");
	}

	static void CompareImages(string path0 = "render.png", string path1 = "ref.png")
	{
		var image0 = TextureGrid<RGB128>.Load(path0);
		var image1 = TextureGrid<RGB128>.Load(path1);

		if (image0.size != image1.size) throw new Exception("Cannot compare two images with different sizes!");

		image0.ForEach(position =>
		{
			Float4 value0 = image0[position];
			Float4 value1 = image1[position];

			image0[position] = (RGB128)(value0 - value1).Absoluted;
		});

		image0.Save("difference.png");
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