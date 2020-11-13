using CodeHelpers.Vectors;
using ForceRenderer.Renderers;

namespace ForceRenderer.Terminals
{
	public class RenderMonitor : Terminal.Section
	{
		public RenderMonitor(Terminal terminal) : base(terminal) { }

		public RenderEngine Engine { get; set; }

		int monitorHeight; //Will be zero if the render engine is not ready, only includes the height of the tiles

		const int StatusHeight = 3; //Lines of text used to display the current status
		public override int Height => monitorHeight + StatusHeight;

		public override void Update()
		{
			int lastHeight = monitorHeight;

			monitorHeight = Engine == null || Engine.CurrentState != RenderEngine.State.rendering ? 0 : Engine.TotalTileSize.y;
			if (lastHeight != monitorHeight) builders.Clear(0); //Clear first row leftover message

			//If engine not ready for displaying
			if (monitorHeight == 0)
			{
				builders.Clear(0);

				const string Message = "Awaiting Render Engine";
				builders.Insert(Int2.zero, Message);

				int periodCount = (int)(terminal.AliveTime / 1000f % 4d);
				for (int i = 0; i < periodCount; i++) builders[new Int2(Message.Length + i, 0)] = '.';

				return;
			}

			foreach (Int2 position in Engine.TotalTileSize.Loop())
			{
				TileWorker worker = Engine.GetWorker(position, out bool completed);
				char character;

				if (worker != null)
				{
					if (!completed)
					{
						double progress = (double)worker.CompletedSample / worker.sampleCount;
						character = (char)Scalars.Lerp('\u258F', '\u2588', (float)progress);
					}
					else character = '\u2593';
				}
				else character = '_';

				builders[position] = character;
			}
		}
	}
}