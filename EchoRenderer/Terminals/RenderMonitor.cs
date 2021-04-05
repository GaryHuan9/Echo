using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Rendering;
using EchoRenderer.Rendering.Tiles;
using EchoRenderer.Textures;

namespace EchoRenderer.Terminals
{
	public class RenderMonitor : Terminal.Section
	{
		public RenderMonitor(Terminal terminal) : base(terminal)
		{
			statusGrid = new string[3][]; //3 rows; 5 columns: Name, Tile, Pixel, Sample, Intersection
			for (int i = 0; i < statusGrid.Length; i++) statusGrid[i] = new string[5];

			statusGrid[0][0] = "";
			statusGrid[0][1] = "Tile";
			statusGrid[0][2] = "Pixel";
			statusGrid[0][3] = "Sample";
			statusGrid[0][4] = "Intersection";

			statusGrid[1][0] = "Per Second";
			statusGrid[2][0] = "Total Done";
		}

		public RenderEngine Engine { get; set; }

		int monitorHeight;          //Will be zero if the render engine is not ready, only includes the height of the tiles
		const int StatusHeight = 6; //Lines of text used to display the current status

		public override int Height => monitorHeight + StatusHeight;
		bool EngineReady => monitorHeight > 0;

		readonly string[][] statusGrid;

		public override void Update()
		{
			if (CheckEngineStatus())
			{
				builders.Clear();
			}

			if (EngineReady)
			{
				DisplayMonitoredStatus();
				DisplayRenderMonitor();
			}
			else DisplayAwaitingStatus();
		}

		/// <summary>
		/// Checks whether the engine is ready to be monitored.
		/// Also assigns <see cref="monitorHeight"/> to the correct value.
		/// Returns true if the status changed from last invoke.
		/// </summary>
		bool CheckEngineStatus()
		{
			bool ready = Engine?.Rendering ?? false;
			bool statusChanged = ready != EngineReady;

			monitorHeight = ready ? Engine.TotalTileSize.y : 0;
			return statusChanged;
		}

		void DisplayAwaitingStatus()
		{
			const string Message = "Awaiting Render Engine";
			const int MaxPeriodCount = 3;

			builders.SetSlice(Int2.zero, Message);

			int periodCount = (int)(terminal.AliveTime / 1000d % (MaxPeriodCount + 1d));
			Span<char> periods = stackalloc char[MaxPeriodCount];

			for (int i = 0; i < periodCount; i++) periods[i] = '.';
			builders.SetSlice(Int2.right * Message.Length, periods);
		}

		void DisplayMonitoredStatus()
		{
			PressedRenderProfile profile = Engine.CurrentProfile;
			PressedScene pressed = profile.scene;
			Texture buffer = profile.renderBuffer;

			//TODO: Use a string builder or something more organized, This is too messy!

			//Display configuration information
			int totalPixel = buffer.size.Product;

			builders.SetLine
			(
				0,
				$"Worker {profile.workerSize}; Resolution {buffer.size}; TotalPX {totalPixel:N0}; PixelSP {profile.pixelSample:N0}; AdaptiveSP {profile.adaptiveSample:N0}; Material {pressed.MaterialCount:N0}; Triangle {pressed.InstancedCounts.triangle:N0}; " +
				$"Sphere {pressed.InstancedCounts.sphere:N0}; Light {pressed.lights.Count:N0}; W/H {buffer.aspect:F2}; Tile {Engine.TotalTileCount:N0}; TileSize {profile.tileSize:N0}; Method {profile.worker};"
			);

			//Display dynamic information
			TimeSpan elapsed = Engine.Elapsed;
			double second = elapsed.TotalSeconds;

			long completedSample = Engine.CompletedSample;
			long completedPixel = Engine.CompletedPixel;
			long rejectedSample = Engine.RejectedSample;

			int completedTile = Engine.CompletedTileCount;
			long intersections = profile.scene.IntersectionPerformed;

			builders.SetLine
			(
				1,
				$"Elapsed {elapsed:hh\\:mm\\:ss\\:ff}; Estimate {TimeSpan.FromSeconds((totalPixel / (completedPixel / second) - second).Clamp(0d, TimeSpan.MaxValue.TotalSeconds)):hh\\:mm\\:ss\\:ff}; Complete% {100d * completedPixel / totalPixel:F2}; CompletedTile {completedTile:N0}; " +
				$"TilePS {completedTile / second:F2}; CompletedSP {completedSample:N0}; RejectedSP {rejectedSample:N0}; SamplePS {completedSample / second:N0}; CompletedPX {completedPixel:N0}; PixelPS {completedPixel / second:N0}; CompletedIS {intersections:N0}; IntersectionPS {intersections / second:N0};"
			);
		}

		void DrawStatusGrid()
		{
			TimeSpan elapsed = Engine.Elapsed;
			double second = elapsed.TotalSeconds;

			Span<long> numbers = stackalloc long[4];

			numbers[0] = Engine.CompletedTileCount;
			numbers[1] = Engine.CompletedPixel;
			numbers[2] = Engine.CompletedSample;
			numbers[3] = Engine.CurrentProfile.scene.IntersectionPerformed;

			for (int i = 1; i <= numbers.Length; i++)
			{
				long number = numbers[i - 1];

				statusGrid[2][i] = number.ToString("N0");
				statusGrid[3][i] = (number / second).ToString("F2");
			}


		}

		void DisplayRenderMonitor()
		{
			const int MarginX = 4;

			Int2 offset = new Int2(MarginX, StatusHeight);
			Int2 tileSize = Engine.TotalTileSize;

			//Display margin
			Span<char> margin = stackalloc char[MarginX];

			for (int i = 0; i < MarginX - 1; i++) margin[i] = '>';
			for (int i = 0; i < tileSize.y; i++) builders.SetSlice(Int2.up * (i + StatusHeight), margin);

			//Display rendering monitor
			foreach (Int2 position in Engine.TotalTileSize.Loop())
			{
				TileWorker worker = Engine.GetWorker(position, out bool completed);
				char character;

				if (worker != null)
				{
					if (!completed)
					{
						double progress = (double)worker.CompletedPixel / worker.TotalPixel;
						character = (char)Scalars.Lerp('\u258F', '\u2588', (float)progress);
					}
					else character = '\u2593';
				}
				else character = '_';

				builders[position + offset] = character;
			}
		}
	}
}