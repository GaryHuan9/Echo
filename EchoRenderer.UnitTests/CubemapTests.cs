using System;
using CodeHelpers.Mathematics;
using EchoRenderer.Core.Texturing;
using EchoRenderer.Core.Texturing.Directional;
using NUnit.Framework;

namespace EchoRenderer.UnitTests;

[TestFixture]
public class CubemapTests : IDirectionalTextureTests
{
	protected override IDirectionalTexture GetTexture()
	{
		Span<Texture> textures = new Texture[Direction.All.Length];

		foreach (ref Texture texture in textures) texture = GenerateRandomTexture((Int2)150);

		return new Cubemap(textures);
	}
}