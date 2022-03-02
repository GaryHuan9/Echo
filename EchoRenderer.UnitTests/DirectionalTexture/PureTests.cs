using EchoRenderer.Core.Texturing;
using EchoRenderer.Core.Texturing.Directional;
using NUnit.Framework;

namespace EchoRenderer.UnitTests.DirectionalTexture;

[TestFixture]
public class PureTests : DirectionalTextureBaseTests
{
	protected override IDirectionalTexture GetTexture() => Texture.normal;
}