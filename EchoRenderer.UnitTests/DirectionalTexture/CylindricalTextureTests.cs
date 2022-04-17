using CodeHelpers.Packed;
using EchoRenderer.Core.Textures.Directional;
using NUnit.Framework;

namespace EchoRenderer.UnitTests.DirectionalTexture;

[TestFixture]
public class CylindricalTextureTests : DirectionalTextureBaseTests
{
	protected override IDirectionalTexture GetTexture() => new CylindricalTexture { Texture = GenerateRandomTexture((Int2)500) };
}