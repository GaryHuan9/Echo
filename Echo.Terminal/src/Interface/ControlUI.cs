using System;
using CodeHelpers.Packed;
using Echo.Terminal.Core;

namespace Echo.Terminal.Interface;

public class ControlUI : AreaUI
{
	public ControlUI() => InvertY = true;

	protected override void Draw(in Domain.Drawer drawer)
	{
		drawer.FillAll();

		Int2 position = Int2.Zero;

		position = drawer.Write(position, "abcdefghijklmnopqrstuvwxyz___" + "abcdefghijklmnopqrstuvwxyz___".ToUpperInvariant(), new TextOptions() { WrapOptions = WrapOptions.LineBreak, Truncate = false });
		position = drawer.WriteLine(position, "1234567890" + "1234567890", new TextOptions() { WrapOptions = WrapOptions.NoWrap, Truncate = false });
	}
}