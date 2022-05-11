using CodeHelpers.Packed;
using Echo.Terminal.Core.Display;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal.Application;

public class CommandTI : AreaTI
{
	protected override void Draw(in Domain.Drawer drawer)
	{
		drawer.FillAll();

		Int2 position = Int2.Zero;

		const string Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
							"Morbi vulputate quam in condimentum ultrices. " +
							"Donec quis tortor ac tellus scelerisque volutpat nec scelerisque ipsum. " +
							"Aenean rhoncus fringilla sollicitudin. ";

		position = drawer.WriteLine(position, Text, new TextOptions { WrapOptions = WrapOptions.NoWrap });
		position = drawer.FillLine(position);

		position = drawer.WriteLine(position, Text, new TextOptions { WrapOptions = WrapOptions.LineBreak });
		position = drawer.FillLine(position);

		position = drawer.WriteLine(position, Text, new TextOptions { WrapOptions = WrapOptions.WordBreak });
		position = drawer.FillLine(position);

		position = drawer.WriteLine(position, Text, new TextOptions { WrapOptions = WrapOptions.Justified });
		position = drawer.FillLine(position);
	}
}