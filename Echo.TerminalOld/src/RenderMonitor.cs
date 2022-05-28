using System;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Common.Mathematics;
using Echo.Core.Evaluation.Engines;
using Echo.Core.Scenic.Geometries;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Grid;

namespace Echo.TerminalOld;

public class RenderMonitor : Terminal.Section
{
	public RenderMonitor(Terminal terminal) : base(terminal)
	{
		statusGrid = new string[gridSize.Y][];
		for (int i = 0; i < statusGrid.Length; i++) statusGrid[i] = new string[gridSize.X];

		statusGrid[0][0] = "";
		statusGrid[0][1] = "Tile";
		statusGrid[0][2] = "Pixel";
		statusGrid[0][3] = "Sample";
		statusGrid[0][4] = "Trace";
		statusGrid[0][5] = "Occlude";

		statusGrid[1][0] = "Per Second";
		statusGrid[2][0] = "Total Done";
		statusGrid[3][0] = "Average Ratio";
		statusGrid[4][0] = "Estimate";
	}

	public TiledRenderEngine Engine { get; set; }

	int monitorHeight; //Will be zero if the render engine is not ready, only includes the height of the tiles

	public override int Height => monitorHeight + StatusHeight;
	bool EngineReady => monitorHeight > 0;

	readonly string[][] statusGrid;

	const int StatusHeight = 8;         //Lines of text used to display the current status
	readonly Int2 gridSize = new(6, 5); //4 rows; 6 columns: Name, Tile, Pixel, Sample, Trace, Occlude

	float Progress => (float)((double)Engine.CompletedPixel / Engine.CurrentProfile.RenderBuffer.size.Product);

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

		monitorHeight = ready ? Engine.TotalTileSize.Y : 0;
		return statusChanged;
	}

	void DisplayAwaitingStatus()
	{
		const string Message = "Awaiting Render Engine";
		const int MaxPeriodCount = 3;

		builders.SetSlice(Int2.Zero, Message);

		int periodCount = (int)(terminal.AliveTime / 1000d % (MaxPeriodCount + 1d));
		Span<char> periods = stackalloc char[MaxPeriodCount];

		for (int i = 0; i < periodCount; i++) periods[i] = '.';
		builders.SetSlice(Int2.Right * Message.Length, periods);
	}

	void DisplayMonitoredStatus()
	{
		TiledRenderProfile profile = Engine.CurrentProfile;
		PreparedScene.Info info = profile.Scene.info;
		RenderBuffer buffer = profile.RenderBuffer;

		//Display configuration information
		int totalPixel = buffer.size.Product;
		GeometryCounts instanced = info.instancedCounts;
		GeometryCounts unique = info.uniqueCounts;

		builders.SetLine(0, $" / Worker {profile.WorkerSize} / Resolution {buffer.size} / Total Pixel {totalPixel:N0} / Total Tile {Engine.TotalTileCount:N0} / Method {profile.Method} / Pixel Sample {profile.PixelSample:N0} / Adaptive Sample {profile.AdaptiveSample:N0} / Tile Size {profile.TileSize:N0} /");
		builders.SetLine(1, $" / Instanced Triangle {instanced.triangle:N0} / Instanced Sphere {instanced.sphere:N0} / Instanced Pack {instanced.instance:N0} / Unique Triangle {unique.triangle:N0} / Unique Sphere {unique.sphere:N0} / Unique Pack {unique.instance:N0} / Material {0:N0} /"); //preparer.materials.Count

		//Display dynamic information
		DrawDynamicLabels();
		DrawStatusGrid();
	}

	void DrawDynamicLabels()
	{
		TimeSpan elapsed = Engine.Elapsed;
		double seconds = elapsed.TotalSeconds;
		long rejectedSample = Engine.RejectedSample;

		double progress = Progress;
		TimeSpan remain = TimeSpan.FromSeconds(seconds / Math.Max(progress, FastMath.Epsilon) - seconds);

		builders.SetLine(2, $" | Time Elapsed {elapsed:hh\\:mm\\:ss\\:ff} | Time Remain {remain:hh\\:mm\\:ss\\:ff} | Complete Percent {progress * 100d:F2}% | Rejected Sample {rejectedSample:N0} |");
	}

	void DrawStatusGrid()
	{
		//Fill the numbers
		double seconds = Engine.Elapsed.TotalSeconds;
		double progress = Progress;

		Span<long> numbers = stackalloc long[]
		{
			Engine.CompletedTileCount,
			Engine.CompletedPixel,
			Engine.CompletedSample,
			Engine.CurrentProfile.Scene.TraceCount,
			Engine.CurrentProfile.Scene.OccludeCount
		};

		Assert.AreEqual(numbers.Length, gridSize.X - 1);

		for (int x = 1; x < gridSize.X; x++)
		{
			long number = numbers[x - 1];
			double rate = number / seconds;
			double estimate = number / progress;

			statusGrid[1][x] = rate.ToString("N2");
			statusGrid[2][x] = $"{number:N0}   ";

			if (x != 1)
			{
				double last = numbers[x - 2];
				double ratio = number / last;

				statusGrid[3][x] = ratio.ToString("N2");
			}
			else statusGrid[3][x] = "";

			statusGrid[4][x] = estimate.ToString("N2");
		}

		//Find max widths for each column
		Span<int> widths = stackalloc int[gridSize.X];

		for (int x = 0; x < gridSize.X; x++)
		{
			ref int width = ref widths[x];
			width = 0;

			for (int y = 0; y < gridSize.Y; y++)
			{
				int length = statusGrid[y][x].Length;
				width = Math.Max(width, length);
			}
		}

		//Draw grid to console
		for (int y = 0; y < gridSize.Y; y++)
		{
			Int2 cursor = new Int2(0, y + 3);
			builders.Clear(cursor.Y);

			for (int x = 0; x < gridSize.X; x++)
			{
				builders.SetSlice(cursor, " | ");
				cursor += Int2.Right * 3;

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

		for (int i = 0; i < MarginX - 1; i++) margin[i] = '=';
		for (int i = 0; i < tileSize.Y; i++) builders.SetSlice(Int2.Up * (i + StatusHeight), margin);

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
			else character = ' ';

			builders[position + offset] = character;
		}
	}
}