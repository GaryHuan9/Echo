using CodeHelpers.Mathematics;
using EchoRenderer.Core.Texturing.Directional;
using NUnit.Framework;

namespace EchoRenderer.UnitTests;

[TestFixture]
public class CylindricalTextureTests : IDirectionalTextureTests
{
	protected override IDirectionalTexture GetTexture() => new CylindricalTexture { Texture = GenerateRandomTexture((Int2)500) };
}