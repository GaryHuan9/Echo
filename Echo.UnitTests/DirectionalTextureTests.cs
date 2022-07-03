using System;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Evaluation.Distributions;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Directional;
using Echo.Core.Textures.Grid;
using NUnit.Framework;

namespace Echo.UnitTests;

[TestFixture]
public class CylindricalTextureTests : DirectionalTextureBaseTests
{
	protected override IDirectionalTexture GetTexture() => new CylindricalTexture { Texture = GenerateRandomTexture((Int2)500) };
}

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

[TestFixture]
public class PureTests : DirectionalTextureBaseTests
{
	protected override IDirectionalTexture GetTexture() => Texture.normal;
}

public abstract class DirectionalTextureBaseTests
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		texture = GetTexture();
		texture.Prepare();
	}

	IDirectionalTexture texture;

	[Test]
	public void Average()
	{
		RGB128 average = texture.ConvergeAverage();
		Assert.That(FastMath.AlmostZero(Difference(average, texture.Average)));
	}

	[Test]
	public void Coherence([Random(0f, 1f, 12)] float x, [Random(0f, 1f, 12)] float y)
	{
		var sample = (Sample2D)new Float2(x, y);

		(RGB128 sampled, float pdf) = texture.Sample(sample, out Float3 incident);

		Assert.That(FastMath.AlmostZero(Difference(sampled, texture.Evaluate(incident))));
		Assert.That(pdf, Is.EqualTo(texture.ProbabilityDensity(incident)).Roughly(0.01f));
	}

	protected abstract IDirectionalTexture GetTexture();

	protected static ArrayGrid<RGB128> GenerateRandomTexture(Int2 maxSize)
	{
		Prng random = Utility.NewRandom();

		var texture = new ArrayGrid<RGB128>(Next2(random, maxSize - Int2.One) + Int2.One);
		texture.ForEach(position => texture.Set(position, (RGB128)(Float4)random.Next3()));

		return texture;

		static Int2 Next2(Prng random, Int2 max) => new(random.Next1(max.X), random.Next1(max.Y));
	}

	static float Difference(in RGB128 value, in RGB128 other) => ((Float4)value - other).SquaredMagnitude;
}