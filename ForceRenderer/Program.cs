using System;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Threads;
using ForceRenderer.IO;
using ForceRenderer.Renderers;
using ForceRenderer.Terminals;
using ForceRenderer.Textures;

namespace ForceRenderer
{
	public class BenchmarkTest
	{
		static readonly float x = 5.64513f;
		static readonly float y = 0.31234f;
		static readonly float z = 7.29368f;
		static readonly float w = 6.86414f;

		static readonly float a = 12345.64513f;
		static readonly float b = 34560.31234f;
		static readonly float c = 25427.29368f;
		static readonly float d = 62345.86414f;

		float x0;
		float y0;
		float z0;
		float w0;

		float a0;
		float b0;
		float c0;
		float d0;

		// [Benchmark]
		public void Native()
		{
			x0 = x;
			y0 = y;
			z0 = z;
			w0 = w;

			a0 = a;
			b0 = b;
			c0 = c;
			d0 = d;

			for (int i = 0; i < 4; i++)
			{
				x0 = MathF.Sqrt(x0);
				y0 = MathF.Sqrt(y0);
				z0 = MathF.Sqrt(z0);
				w0 = MathF.Sqrt(w0);

				a0 = MathF.Sqrt(a0);
				b0 = MathF.Sqrt(b0);
				c0 = MathF.Sqrt(c0);
				d0 = MathF.Sqrt(d0);
			}
		}

		// [Benchmark]
		public unsafe void Intrinsic()
		{
			fixed (float* pointer = &x)
			{
				Vector256<float> vector = Avx.LoadVector256(pointer);

				vector = Avx.Sqrt(vector);
				vector = Avx.Sqrt(vector);
				vector = Avx.Sqrt(vector);
				vector = Avx.Sqrt(vector);

				x0 = vector.GetElement(0);
				y0 = vector.GetElement(1);
				z0 = vector.GetElement(2);
				w0 = vector.GetElement(3);

				a0 = vector.GetElement(4);
				b0 = vector.GetElement(5);
				c0 = vector.GetElement(6);
				d0 = vector.GetElement(7);
			}
		}

		static readonly Float4 first0 = new Float4(x, y, z, w);
		static readonly Float4 first1 = new Float4(a, b, c, d);
		static Float4 result0;

		static readonly Vector4 second0 = new Vector4(x, y, z, w);
		static readonly Vector4 second1 = new Vector4(a, b, c, d);
		static Vector4 result1;

		[Benchmark]
		public void Mine()
		{
			result0 = first0 / first1;
		}

		[Benchmark]
		public void SeeSharp()
		{
			result1 = second0 / second1;
		}

		[Benchmark]
		public void Native2()
		{
			x0 = x / a;
			y0 = y / b;
			z0 = z / c;
			w0 = w / d;
		}
	}

	public class Program
	{
		static void Main()
		{
			BenchmarkRunner.Run<BenchmarkTest>();

			return;
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
				new Int2(320, 180), new Int2(854, 480), new Int2(1920, 1080),
				new Int2(3840, 2160), new Int2(1024, 1024), new Int2(512, 512)
			};

			RenderTexture buffer = new RenderTexture(resolutions[1]);
			using RenderEngine engine = new RenderEngine
										{
											RenderBuffer = buffer, Scene = new SingleBMWScene(),
											PixelSample = 128, AdaptiveSample = 12000, TileSize = 32
										};

			renderEngine = engine;
			renderDisplay.Engine = engine;

			engine.Begin();
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