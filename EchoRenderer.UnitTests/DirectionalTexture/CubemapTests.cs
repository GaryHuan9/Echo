using System;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Core.Textures;
using EchoRenderer.Core.Textures.Directional;
using NUnit.Framework;

namespace EchoRenderer.UnitTests.DirectionalTexture;

[TestFixture]
public class CubemapTests : DirectionalTextureBaseTests
{
	protected override IDirectionalTexture GetTexture()
	{
		Span<Texture> textures = new Texture[Direction.All.Length];

		foreach (ref Texture texture in textures) texture = GenerateRandomTexture((Int2)150);

		return new Cubemap(textures);
	}
}