using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Objects.Scenes;
using EchoRenderer.Rendering;
using EchoRenderer.Rendering.Engines;
using EchoRenderer.Rendering.Engines.Tiles;
using EchoRenderer.Textures;

namespace EchoRenderer.Terminals
{
	public class RenderMonitor : Terminal.Section
	{
		public RenderMonitor(Terminal terminal) : base(terminal)
		{
			statusGrid = new string[gridSize.y][];
			for (int i = 0; i < statusGrid.Length; i++) statusGrid[i] = new string[gridSize.x];

			statusGrid[0][0] = "";
			statusGrid[0][1] = "Tile";
			statusGrid[0][2] = "Pixel";
			statusGrid[0][3] = "Sample";
			statusGrid[0][4] = "Intersection";

			statusGrid[1][0] = "Per Second";
			statusGrid[2][0] = "Total Done";
			statusGrid[3][0] = "Average Ratio";
		}

		public TiledRenderEngine Engine { get; set; }

		int monitorHeight; //Will be zero if the render engine is not ready, only includes the height of the tiles

		public override int Height => monitorHeight + StatusHeight;
		bool EngineReady => monitorHeight > 0;

		readonly string[][] statusGrid;

		const int StatusHeight = 7;              //Lines of text used to display the current status
		readonly Int2 gridSize = new Int2(5, 4); //4 rows; 5 columns: Name, Tile, Pixel, Sample, Intersection

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
			TiledRenderProfile profile = Engine.CurrentProfile;
			PressedScene pressed = profile.Scene;
			Texture2D buffer = profile.RenderBuffer;

			//Display configuration information
			int totalPixel = buffer.size.Product;
			GeometryCounts instanced = pressed.InstancedCounts;
			GeometryCounts unique = pressed.UniqueCounts;

			builders.SetLine(0, $" / Worker {profile.WorkerSize} / Resolution {buffer.size} / Total Pixel {totalPixel:N0} / Total Tile {Engine.TotalTileCount:N0} / Method {profile.Method} / Pixel Sample {profile.PixelSample:N0} / Adaptive Sample {profile.AdaptiveSample:N0} / Tile Size {profile.TileSize:N0} /");
			builders.SetLine(1, $" / Instanced Triangle {instanced.triangle:N0} / Instanced Sphere {instanced.sphere:N0} / Instanced Pack {instanced.pack:N0} / Unique Triangle {unique.triangle:N0} / Unique Sphere {unique.sphere:N0} / Unique Pack {unique.pack:N0} / Material {pressed.MaterialCount:N0} / Light {pressed.lights.Count:N0} /");

			//Display dynamic information
			DrawDynamicLabels();
			DrawStatusGrid();
		}

		void DrawDynamicLabels()
		{
			TimeSpan elapsed = Engine.Elapsed;
			double seconds = elapsed.TotalSeconds;

			long completedPixel = Engine.CompletedPixel;
			long rejectedSample = Engine.RejectedSample;

			double fraction = (double)completedPixel / Engine.CurrentProfile.RenderBuffer.size.Product;
			TimeSpan remain = TimeSpan.FromSeconds(seconds / Math.Max(fraction, Scalars.Epsilon) - seconds);

			builders.SetLine(2, $" | Time Elapsed {elapsed:hh\\:mm\\:ss\\:ff} | Time Remain {remain:hh\\:mm\\:ss\\:ff} | Complete Percent {fraction * 100d:F2}% | Rejected Sample {rejectedSample:N0} |");
		}

		void DrawStatusGrid()
		{
			//Fill the numbers
			Span<long> numbers = stackalloc long[gridSize.x - 1];
			double seconds = Engine.Elapsed.TotalSeconds;

			numbers[0] = Engine.CompletedTileCount;
			numbers[1] = Engine.CompletedPixel;
			numbers[2] = Engine.CompletedSample;
			numbers[3] = Engine.CurrentProfile.Scene.Intersections;

			for (int x = 1; x < gridSize.x; x++)
			{
				long number = numbers[x - 1];
				double rate = number / seconds;

				statusGrid[1][x] = rate.ToString("N2");
				statusGrid[2][x] = $"{number:N0}   ";

				if (x != 1)
				{
					double last = numbers[x - 2];
					double ratio = number / last;

					statusGrid[3][x] = ratio.ToString("N2");
				}
				else statusGrid[3][x] = "";
			}

			//Find max widths for each column
			Span<int> widths = stackalloc int[gridSize.x];

			for (int x = 0; x < gridSize.x; x++)
			{
				ref int width = ref widths[x];
				width = 0;

				for (int y = 0; y < gridSize.y; y++)
				{
					int length = statusGrid[y][x].Length;
					width = Math.Max(width, length);
				}
			}

			//Draw grid to console
			for (int y = 0; y < gridSize.y; y++)
			{
				Int2 cursor = new Int2(0, y + 3);
				builders.Clear(cursor.y);

				for (int x = 0; x < gridSize.x; x++)
				{
					builders.SetSlice(cursor, " | ");
					cursor += Int2.right * 3;

					string label = statusGrid[y][x];
					int width = widths[x];

					builders.SetSlice(cursor + new Int2(width - label.Length, 0), label);
					cursor += new Int2(width, 0);
				}

				builders.SetSlice(cursor, " | ");
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