using Echo.Core.Textures;
using Echo.Core.Textures.Directional;
using NUnit.Framework;

namespace Echo.UnitTests.DirectionalTexture;

[TestFixture]
public class PureTests : DirectionalTextureBaseTests
{
	protected override IDirectionalTexture GetTexture() => Texture.normal;
}