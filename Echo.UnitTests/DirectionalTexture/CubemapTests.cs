using System;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Textures;
using Echo.Core.Textures.Directional;
using NUnit.Framework;

namespace Echo.UnitTests.DirectionalTexture;

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