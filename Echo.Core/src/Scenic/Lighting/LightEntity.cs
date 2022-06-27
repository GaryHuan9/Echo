using Echo.Core.Textures.Colors;

namespace Echo.Core.Scenic.Lighting;

public class LightEntity : Entity
{
	/// <summary>
	/// The main color and intensity of this <see cref="LightEntity"/>.
	/// </summary>
	public RGB128 Intensity { get; set; } = RGB128.White;
}