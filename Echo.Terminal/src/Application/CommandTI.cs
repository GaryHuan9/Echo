using CodeHelpers.Packed;
using Echo.Terminal.Core.Display;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal.Application;

public class CommandTI : AreaTI
{
	protected override void Paint(in Painter painter)
	{
		painter.FillAll();

		Int2 position = Int2.Zero;

		const string Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
							"Morbi vulputate quam in condimentum ultrices. " +
							"Donec quis tortor ac tellus scelerisque volutpat nec scelerisque ipsum. " +
							"Aenean rhoncus fringilla sollicitudin. ";

		position = painter.WriteLine(position, Text, new TextOptions { WrapOptions = WrapOptions.NoWrap });
		position = painter.FillLine(position);

		position = painter.WriteLine(position, Text, new TextOptions { WrapOptions = WrapOptions.LineBreak });
		position = painter.FillLine(position);

		position = painter.WriteLine(position, Text, new TextOptions { WrapOptions = WrapOptions.WordBreak });
		position = painter.FillLine(position);

		position = painter.WriteLine(position, Text, new TextOptions { WrapOptions = WrapOptions.Justified });
		position = painter.FillLine(position);
	}
}