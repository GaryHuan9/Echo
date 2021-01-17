using System;
using System.Linq;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using ForceRenderer.Renderers;
using ForceRenderer.Terminals;
using ForceRenderer.Textures;

namespace ForceRenderer
{
	public class Program
	{
		static void Main()
		{
			using Terminal terminal = new Terminal();
			renderTerminal = terminal;

			var commandsController = new CommandsController(terminal);
			var renderDisplay = new RenderMonitor(terminal);

			terminal.AddSection(commandsController);
			terminal.AddSection(renderDisplay);

			ThreadHelper.MainThread = Thread.CurrentThread;
			RandomHelper.Seed = 47;

			//Render
			Int2[] resolutions =
			{
				new Int2(320, 180), new Int2(854, 480), new Int2(1920, 1080),
				new Int2(3840, 2160), new Int2(1024, 1024), new Int2(512, 512)
			};

			RenderTexture buffer = new RenderTexture(resolutions[1]);
			using RenderEngine engine = new RenderEngine
										{
											RenderBuffer = buffer, Scene = new SingleDragonScene(),
											PixelSample = 32, AdaptiveSample = 400, TileSize = 32
										};

			renderEngine = engine;
			renderDisplay.Engine = engine;

			PerformanceTest setupTest = new PerformanceTest();
			using (setupTest.Start()) engine.Begin();

			commandsController.Log($"Engine Setup Complete: {setupTest.ElapsedMilliseconds}ms");
			engine.WaitForRender();

			//Copies render texture and saves as file
			Texture2D output = new Texture2D(buffer);

			output.SetReadonly();
			output.Save("render.png");
			buffer.Save("render.rdt");

			//Logs render stats
			double elapsedSeconds = engine.Elapsed.TotalSeconds;
			long completedSample = engine.CompletedSample;

			commandsController.Log(new Float3(buffer.Max(pixel => pixel.x), buffer.Max(pixel => pixel.y), buffer.Max(pixel => pixel.z)).ToString());
			commandsController.Log($"Completed after {elapsedSeconds:F2} seconds with {completedSample:N0} samples at {completedSample / elapsedSeconds:N0} samples per second.");

			renderEngine = null;
			Console.ReadKey();
		}

		static RenderEngine renderEngine;
		static Terminal renderTerminal;

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
	}
}