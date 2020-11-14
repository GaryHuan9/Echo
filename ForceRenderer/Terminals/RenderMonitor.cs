using System;
using CodeHelpers.Vectors;
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
			bool ready = Engine?.CurrentState == RenderEngine.State.rendering;
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
			long totalSample = buffer.size.Product * profile.pixelSample;

			builders.SetSlice
			(
				new Int2(0, 0),
				$"Worker {profile.workerSize}; Res {buffer.size}; SPP {profile.pixelSample}; TotalSP {totalSample}; Bundle {pressed.bundleCount}; " +
				$"Light {(pressed.directionalLight == null ? 0 : 1)}; W/H {buffer.aspect}; Tile {Engine.TotalTileCount}; TileSize {Engine.TileSize}; Method {Engine.PixelWorker}; "
			);

			//Display dynamic information
			TimeSpan elapsed = Engine.Elapsed;
			double second = elapsed.TotalSeconds;

			int dispatchedTile = Engine.DispatchedTileCount;
			int completedTile = Engine.CompletedTileCount;

			long initiated = Engine.InitiatedSample;
			long completed = Engine.CompletedSample;

			double estimate = Math.Max(0d, totalSample / (completed / second) - second);

			builders.SetSlice
			(
				new Int2(0, 1),
				$"Elapsed {elapsed:hh\\:mm\\:ss\\:ff}; CompleteSP {completed}; RenderSP {initiated - completed}; CompleteTile {completedTile}; Estimate {TimeSpan.FromSeconds(estimate):hh\\:mm\\:ss\\:ff}; " +
				$"Complete% {100d * completed / totalSample:F2}; SPPS {completed / second:F2}; CompleteTilePS {completedTile / second:F2}; DispatchTilePS {dispatchedTile / second:F2}; "
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
						if (worker.CompletedSample != worker.sampleCount)
						{
							double progress = (double)worker.CompletedSample / worker.sampleCount;
							character = (char)Scalars.Lerp('\u258F', '\u2588', (float)progress);
						}
						else character = 'S';
					}
					else character = '\u2593';
				}
				else character = '_';

				builders[position + offset] = character;
			}
		}
	}
}