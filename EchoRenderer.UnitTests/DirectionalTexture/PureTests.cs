using EchoRenderer.Core.Textures;
using EchoRenderer.Core.Textures.Directional;
using NUnit.Framework;

namespace EchoRenderer.UnitTests.DirectionalTexture;

[TestFixture]
public class PureTests : DirectionalTextureBaseTests
{
	protected override IDirectionalTexture GetTexture() => Texture.normal;
}