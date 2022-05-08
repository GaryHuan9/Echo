namespace Echo.Terminal.Areas;

public sealed class FilledUI : AreaUI
{
	public char Filling { get; set; } = ' ';

	public override void Update() => Domain.Fill(Filling);
}