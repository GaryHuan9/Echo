using CodeHelpers.Packed;
using Echo.Terminal.Core.Display;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal.Application;

public class CommandTI : AreaTI
{
	public CommandTI() => InvertY = true;

	protected override void Draw(in Domain.Drawer drawer)
	{
		drawer.FillAll();

		Int2 position = Int2.Zero;

		const string Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi vulputate quam in condimentum ultrices. Aenean quis tortor ac tellus scelerisque volutpat nec scelerisque ipsum. Donec at eleifend ipsum, sit amet blandit nisi. Aenean rhoncus fringilla sollicitudin. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas faucibus congue vehicula. Curabitur molestie malesuada risus. Donec maximus dui urna, eget aliquam felis fringilla at. Praesent varius rutrum magna non sollicitudin. Nullam in purus sit amet tellus elementum imperdiet at sit amet diam. Vestibulum consectetur lacus magna, sed dignissim justo suscipit eu. Maecenas sed odio nulla. Donec tristique elit et metus iaculis pretium.";

		position = drawer.WriteLine(position, Text, new TextOptions { WrapOptions = WrapOptions.LineBreak, Truncate = false });
		position = drawer.WriteLine(position, Text, new TextOptions { WrapOptions = WrapOptions.NoWrap, Truncate = false });
	}
}