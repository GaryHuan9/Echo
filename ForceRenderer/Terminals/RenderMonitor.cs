using System;
using CodeHelpers.Mathematics;
using ForceRenderer.IO;
using ForceRenderer.Renderers;

namespace ForceRenderer.Terminals
{
	public class RenderMonitor : Terminal.Section
	{
		public RenderMonitor(Terminal terminal) : base(terminal) { }

		public RenderEngine Engine { get; set; }

		int monitorHeight;          //Will be zero if the render engine is not ready, only includes the height of the tiles
		const int StatusHeight = 2; //Lines of text used to display the current status

		public override int Height => monitorHeight + StatusHeight;
		bool EngineReady => monitorHeight > 0;

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
			RenderEngine.Profile profile = Engine.CurrentProfile;
			Texture buffer = Engine.RenderBuffer;
			PressedScene pressed = profile.pressed;

			//Display configuration information
			int totalPixel = buffer.size.Product;

			builders.SetLine
			(
				0,
				$"Worker {profile.workerSize}; Resolution {buffer.size}; TotalPX {totalPixel:N0}; PixelSP {profile.pixelSample:N0}; AdaptiveSP {profile.adaptiveSample:N0}; Material {pressed.MaterialCount:N0}; Triangle {pressed.TriangleCount:N0}; " +
				$"Sphere {pressed.SphereCount:N0}; Light {(pressed.directionalLight.direction == default ? 0 : 1):N0}; W/H {buffer.aspect:F2}; Tile {Engine.TotalTileCount:N0}; TileSize {profile.tileSize:N0}; Method {Engine.PixelWorker};"
			);

			//Display dynamic information
			TimeSpan elapsed = Engine.Elapsed;
			double second = elapsed.TotalSeconds;

			long completedSample = Engine.CompletedSample;
			long completedPixel = Engine.CompletedPixel;

			int completedTile = Engine.CompletedTileCount;

			builders.SetLine
			(
				1,
				$"Elapsed {elapsed:hh\\:mm\\:ss\\:ff}; Estimate {TimeSpan.FromSeconds((totalPixel / (completedPixel / second) - second).Clamp(0d, TimeSpan.MaxValue.TotalSeconds)):hh\\:mm\\:ss\\:ff}; Complete% {100d * completedPixel / totalPixel:F2}; " +
				$"CompleteTile {completedTile:N0}; CompleteTilePS {completedTile / second:F2}; CompletedSP {completedSample:N0}; CompletedPX {completedPixel:N0}; SamplePS {completedSample / second:N0}; PixelPS {completedPixel / second:N0};"
			);
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