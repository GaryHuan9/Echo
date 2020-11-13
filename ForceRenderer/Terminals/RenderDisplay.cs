using CodeHelpers.Vectors;
using ForceRenderer.Renderers;

namespace ForceRenderer.Terminals
{
	public class RenderDisplay : Terminal.Section
	{
		public RenderDisplay(Terminal terminal) : base(terminal) { }

		public RenderEngine Engine { get; set; }

		bool IsEngineReady => Engine != null && Engine.CurrentState == RenderEngine.State.rendering;
		public override int Height => IsEngineReady ? Engine.TotalTileSize.y : 2;

		public override void Update()
		{
			if (!IsEngineReady)
			{
				builders.Clear(0);

				const string Message = "Awaiting Render Engine";
				builders.Insert(Int2.zero, Message);

				int periodCount = (int)(terminal.AliveTime / 1000f % 4d);
				for (int i = 0; i < periodCount; i++) builders[new Int2(Message.Length + i, 0)] = '.';

				return;
			}

			builders.Clear(0); //Clear first row leftover message

			foreach (Int2 position in Engine.TotalTileSize.Loop())
			{
				TileWorker worker = Engine.GetWorker(position, out bool completed);

				if (worker != null)
				{
					float progress;

					if (completed) progress = 1f;
					else progress = (float)worker.CompletedSample / worker.sampleCount;

					builders[position] = GetProgressCharacter(progress);
				}
				else builders[position] = '_';
			}
		}

		static char GetProgressCharacter(float progress) => (char)Scalars.Lerp('\u258F', '\u2588', progress);
	}
}