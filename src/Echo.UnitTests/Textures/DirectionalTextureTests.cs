using System;
using System.Collections.Generic;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Mathematics.Randomization;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Directional;
using Echo.Core.Textures.Grids;
using NUnit.Framework;

namespace Echo.UnitTests.Textures;

[TestFixture]
public class CylindricalTextureTests : DirectionalTextureBaseTests
{
	static CylindricalTextureTests()
	{
		var distribution = new StratifiedDistribution { Extend = 100, Prng = new SystemPrng(1) };

		distribution.BeginSeries(Int2.Zero);

		for (int i = 0; i < distribution.Extend; i++)
		{
			distribution.BeginSession();
			directionInputs.Add(distribution.Next2D().UniformSphere);
		}

		distribution = distribution with { Prng = new SystemPrng(2) };

		distribution.BeginSeries(Int2.Zero);

		for (int i = 0; i < distribution.Extend; i++)
		{
			distribution.BeginSession();
			uvInputs.Add(distribution.Next2D());
		}
	}

	protected override IDirectionalTexture GetTexture() => new CylindricalTexture { Texture = GenerateRandomTexture((Int2)500) };

	static readonly List<Float3> directionInputs = new();
	static readonly List<Float2> uvInputs = new();

	[Test]
	public void ToUV([ValueSource(nameof(directionInputs))] Float3 direction)
	{
		Float2 uv = CylindricalTexture.ToUV(direction);
		Float3 check = CylindricalTexture.ToDirection(uv);
		Assert.That(FastMath.AlmostZero(check.SquaredDistance(direction)));
	}

	[Test]
	public void ToDirection([ValueSource(nameof(uvInputs))] Float2 uv)
	{
		Float3 direction = CylindricalTexture.ToDirection(uv);
		Float2 check = CylindricalTexture.ToUV(direction);
		Assert.That(check, Is.EqualTo(uv));
	}
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
	protected override IDirectionalTexture GetTexture() => Pure.normal;
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
		RGB128 average = texture.AverageConverge();
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

	static float Difference(RGB128 value, RGB128 other) => ((Float4)value - other).SquaredMagnitude;
}