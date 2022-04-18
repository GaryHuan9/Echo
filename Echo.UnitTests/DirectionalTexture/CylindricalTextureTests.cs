using CodeHelpers.Packed;
using Echo.Core.Textures.Directional;
using NUnit.Framework;

namespace Echo.UnitTests.DirectionalTexture;

[TestFixture]
public class CylindricalTextureTests : DirectionalTextureBaseTests
{
	protected override IDirectionalTexture GetTexture() => new CylindricalTexture { Texture = GenerateRandomTexture((Int2)500) };
}