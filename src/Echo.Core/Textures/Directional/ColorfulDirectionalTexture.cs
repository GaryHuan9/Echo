using Echo.Core.Common.Packed;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures.Directional;

[EchoSourceUsable]
public class ColorfulDirectionalTexture : IDirectionalTexture
{
	public RGB128 Average => new(1f / 3f);

	public RGB128 Evaluate(Float3 incident)
	{
		Float4 color = (Float4)incident;
		return (RGB128)(color * color);
	}
}