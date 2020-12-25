using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Mathematics.Enumerables;
using CodeHelpers.Threads;
using ForceRenderer.IO;
using ForceRenderer.Mathematics;
using ForceRenderer.Objects.SceneObjects;
using ForceRenderer.Renderers;
using ForceRenderer.Terminals;

namespace ForceRenderer
{
	public class Program
	{
		static void Main()
		{
			// var source = new Texture("render.png");
			// var destination = new Texture(source.size);
			//
			// var denoiser = new Denoiser(source, destination);
			// denoiser.Dispatch();
			//
			// destination.SaveFile("denoised.png");
			//
			// return;

			// AxisAlignedBoundingBox[] aabbs =
			// {
			// 	new AxisAlignedBoundingBox(Float3.up, Float3.half),
			// 	new AxisAlignedBoundingBox(Float3.one * 2, Float3.half),
			// 	new AxisAlignedBoundingBox(Float3.right * 2, Float3.half),
			// 	new AxisAlignedBoundingBox(Float3.down * 2, Float3.half)
			// };
			//
			// var bvh = new BoundingVolumeHierarchy(null, aabbs, Enumerable.Range(0, aabbs.Length).ToList());
			//
			// var aabb = new AxisAlignedBoundingBox(Float3.zero, new Float3(0.5f, 0f, 0.5f));
			// Ray ray = new Ray(Float3.up, Float3.down);
			//
			// DebugHelper.Log(aabb.Intersect(ray));
			//
			// return;

			Terminal terminal = new Terminal();

			var commandsController = new CommandsController(terminal);
			var renderDisplay = new RenderMonitor(terminal);

			terminal.AddSection(commandsController);
			terminal.AddSection(renderDisplay);

			ThreadHelper.MainThread = Thread.CurrentThread;
			RandomHelper.Seed = 47;

			//Render
			Int2[] resolutions =
			{
				new Int2(320, 180), new Int2(854, 480), new Int2(1920, 1080), new Int2(3840, 2160), new Int2(1024, 1024), new Int2(512, 512)
			};

			Texture buffer = new Texture(resolutions[1]);
			using RenderEngine engine = new RenderEngine
										{
											RenderBuffer = buffer, Scene = new TestTexture(),
											PixelSample = 32, AdaptiveSample = 256, TileSize = 32
										};

			renderEngine = engine;
			renderDisplay.Engine = engine;

			engine.Begin();
			engine.WaitForRender();

			buffer.SaveFile("render.png");

			double elapsedSeconds = engine.Elapsed.TotalSeconds;
			long completedSample = engine.CompletedSample;

			commandsController.Log($"Completed after {elapsedSeconds:F2} seconds with {completedSample:N0} samples at {completedSample / elapsedSeconds:N0} samples per second.");

			renderEngine = null;
			Thread.Sleep((int)(elapsedSeconds * 1000));
		}

		static RenderEngine renderEngine;

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
	}
}