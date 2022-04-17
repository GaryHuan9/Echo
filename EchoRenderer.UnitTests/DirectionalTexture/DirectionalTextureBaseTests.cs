using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Randomization;
using EchoRenderer.Core.Evaluation.Distributions;
using EchoRenderer.Core.Textures.Colors;
using EchoRenderer.Core.Textures.Directional;
using EchoRenderer.Core.Textures.Grid;
using NUnit.Framework;

namespace EchoRenderer.UnitTests.DirectionalTexture;

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
		IRandom random = Utilities.NewRandom();

		var texture = new ArrayGrid<RGB128>(Next2(random, maxSize - Int2.One) + Int2.One);
		texture.ForEach(position => texture[position] = (RGB128)(Float4)random.Next3());

		return texture;

		static Int2 Next2(IRandom random, Int2 max) => new(random.Next1(max.X), random.Next1(max.Y));
	}

	static float Difference(in RGB128 value, in RGB128 other) => ((Float4)value - other).SquaredMagnitude;
}