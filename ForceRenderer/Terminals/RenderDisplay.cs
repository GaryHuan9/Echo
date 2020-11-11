using System.Text;
using CodeHelpers;
using ForceRenderer.Renderers;

namespace ForceRenderer.Terminals
{
	public class RenderDisplay : Terminal.Section
	{
		public RenderDisplay(Terminal terminal, MinMaxInt displayDomain) : base(terminal, displayDomain) { }

		RenderEngine _engine;

		public RenderEngine Engine
		{
			get => _engine;
			set
			{
				if (_engine == value) return;
				_engine = value;


			}
		}

		public override void Update()
		{
			// for (char i = '\u2588'; i <= '\u258F'; i++)
			// {
			// 	Console.WriteLine(i);
			// }

			// StringBuilder builder = this[0];

			// builder.Clear();
			// if (Engine != null) builder.Append($"{Engine.DispatchedTileCount} / {Engine.TotalTileCount} Tiles Dispatched");
		}
	}
}