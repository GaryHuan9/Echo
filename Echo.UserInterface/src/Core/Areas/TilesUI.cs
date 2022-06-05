namespace Echo.UserInterface.Core.Areas;

public class TilesUI : AreaUI
{
	public TilesUI() : base("Tiles")
	{
		// IntPtr texture = SDL_CreateTexture
		// (
		// 	IntPtr.Zero, SDL_PIXELFORMAT_RGBA8888,
		// 	(int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
		// 	64, 64
		// );
	}

	protected override void Draw()
	{
		// if (Device.Instance?.StartedOperation is not TiledEvaluationOperation { CurrentProfile.Buffer: { } buffer }) return;
	}
}