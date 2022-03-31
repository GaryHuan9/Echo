using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Packed;
using EchoRenderer.Common.Mathematics.Randomization;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Texturing.Directional;
using EchoRenderer.Core.Texturing.Grid;
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
		Vector128<float> average = texture.ConvergeAverage();
		Assert.That(Difference(average, texture.Average), Is.LessThan(0.002f));
	}

	[Test]
	public void Coherence([Random(0f, 1f, 12)] float x, [Random(0f, 1f, 12)] float y)
	{
		Sample2D sample = (Sample2D)new Float2(x, y);

		Vector128<float> sampled = texture.Sample(sample, out Float3 incident);

		Assert.That(Difference(sampled, texture.Evaluate(incident)), Is.LessThan(0.002f));
		Assert.That(pdf, Is.EqualTo(texture.ProbabilityDensity(incident)).Roughly(0.01f));
	}

	protected abstract IDirectionalTexture GetTexture();

	protected static ArrayGrid GenerateRandomTexture(Int2 maxSize)
	{
		IRandom random = Utilities.NewRandom();

		ArrayGrid texture = new ArrayGrid(Next2(random, maxSize - Int2.One) + Int2.One);
		texture.ForEach(position => texture[position] = Common.Utilities.ToVector(random.Next3()));

		return texture;

		static Int2 Next2(IRandom random, Int2 max) => new(random.Next1(max.X), random.Next1(max.Y));
	}

	static float Difference(in Vector128<float> value, in Vector128<float> other)
	{
		Vector128<float> difference = Sse.Subtract(value, other);

		Vector128<float> sign = Sse.CompareGreaterThan(difference, Vector128<float>.Zero);
		sign = Sse.Subtract(Sse.Multiply(sign, Vector128.Create(2f)), Vector128.Create(1f));
		difference = Sse.Multiply(sign, difference);

		return difference.GetElement(0) + difference.GetElement(1) + difference.GetElement(2) + difference.GetElement(3);
	}
}