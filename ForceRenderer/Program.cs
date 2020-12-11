using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Collections;
using CodeHelpers.Threads;
using CodeHelpers.Vectors;
using ForceRenderer.CodeHelpers.Vectors;
using ForceRenderer.IO;
using ForceRenderer.Objects;
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
			// Console.WriteLine(aabb.Intersect(ray));
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
				new Int2(320, 180), new Int2(854, 480), new Int2(1920, 1080), new Int2(3840, 2160), new Int2(1000, 1000), new Int2(500, 500)
			};

			Texture buffer = new Texture(resolutions[4]);
			using RenderEngine engine = new RenderEngine
										{
											RenderBuffer = buffer, Scene = new CornellBox(),
											PixelSample = 100, TileSize = 50
										};

			renderEngine = engine;
			renderDisplay.Engine = engine;

			engine.Begin();
			engine.WaitForRender();

			Texture noisy = new Texture(buffer.size);
			new Denoiser(buffer, noisy).Dispatch();

			noisy.SaveFile("noisy.png");
			buffer.SaveFile("render.png");

			commandsController.Log($"Completed in {engine.Elapsed.TotalMilliseconds}ms");
			renderEngine = null;
		}

		static RenderEngine renderEngine;

		[Command]
		static CommandResult DoStuff(int value)
		{
			// renderEngine.
			throw new NotImplementedException();
		}
	}
}