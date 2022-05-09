namespace Echo.Terminal.Core;

public sealed class FilledUI : AreaUI
{
	public char Filling { get; set; } = ' ';

	protected override void Draw(in Domain.Drawer drawer) => drawer.FillAll(Filling);
}