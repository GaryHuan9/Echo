using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics.Randomization;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Texturing.Directional;
using EchoRenderer.Core.Texturing.Grid;
using NUnit.Framework;

namespace EchoRenderer.UnitTests;

public abstract class IDirectionalTextureTests
{
	[SetUp]
	// [Repeat(100)]
	public void SetUp()
	{
		texture = GetTexture();
		texture.Prepare();
	}

	IDirectionalTexture texture;

	[Test]
	public void Average()
	{
		Vector128<float> average = texture.ConvergeAverage();
		Assert.That(Difference(average, texture.Average), Is.LessThan(0.001f));
	}

	[Test]
	public void Coherence([Random(0f, 1f, 4)] float x, [Random(0f, 1f, 4)] float y)
	{
		Distro2 distro = (Distro2)new Float2(x, y);

		Vector128<float> sampled = texture.Sample(distro, out Float3 incident, out float pdf);

		Assert.That(Difference(sampled, texture.Evaluate(incident)), Is.LessThan(0.001f));
		Assert.That(pdf, Is.EqualTo(texture.ProbabilityDensity(incident)).Roughly(8));
	}

	protected abstract IDirectionalTexture GetTexture();

	protected static ArrayGrid GenerateRandomTexture(Int2 maxSize)
	{
		IRandom random = Utilities.NewRandom();

		ArrayGrid texture = new ArrayGrid(new Int2(random.Next1(maxSize.x), random.Next1(maxSize.y)));
		texture.ForEach(position => texture[position] = Common.Utilities.ToVector(random.Next3()));

		return texture;
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