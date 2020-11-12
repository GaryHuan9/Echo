using System.Text;
using CodeHelpers;
using CodeHelpers.Vectors;
using ForceRenderer.Renderers;

namespace ForceRenderer.Terminals
{
	public class RenderDisplay : Terminal.Section
	{
		public RenderDisplay(Terminal terminal, MinMaxInt displayDomain) : base(terminal, displayDomain) { }

		public RenderEngine Engine { get; set; }

		public override void Update()
		{
			if (Engine == null || Engine.CurrentState != RenderEngine.State.rendering)
			{
				var builder = this[0];

				builder.Clear();
				builder.Append("Awaiting Render Engine");

				int periodCount = (int)(terminal.AliveTime / 1000f % 4d);
				for (int i = 0; i < periodCount; i++) builder.Append('.');

				return;
			}

			Int2 tileSize = Engine.TotalTileSize;

			if (tileSize.y > Height)
			{
				var builder = this[0];
				builder.Clear();

				builder.Append("Insufficient render display height! ");
				builder.Append($"Tile size: {tileSize}; Display height: {Height}");
				return;
			}

			for (int y = 0; y < tileSize.y; y++)
			{
				var builder = this[y];
				builder.Clear();

				for (int x = 0; x < tileSize.x; x++)
				{
					TileWorker worker = Engine.GetWorker(new Int2(x, y), out bool completed);

					if (!completed)
					{
						if (worker != null)
						{
							double progress = (double)worker.CompletedSample / worker.sampleCount;
							builder.Append(GetProgressCharacter((float)progress));
						}
						else builder.Append(' ');
					}
					else builder.Append('#');
				}
			}
		}

		static char GetProgressCharacter(float progress) => (char)Scalars.Lerp('\u258F', '\u2588', progress);
	}
}