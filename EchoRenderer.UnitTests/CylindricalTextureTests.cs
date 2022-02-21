using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics.Randomization;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Texturing.Directional;
using EchoRenderer.Core.Texturing.Grid;
using NUnit.Framework;

namespace EchoRenderer.UnitTests;

public class CylindricalTextureTests
{
	[SetUp]
	[Repeat(100)]
	public void SetUp()
	{
		texture = new CylindricalTexture { Texture = GenerateRandomTexture((Int2)500) };
		texture.Prepare();
	}

	CylindricalTexture texture;

	[Test]
	public void Average()
	{
		Vector128<float> average = CalculateAverage(texture);
		Assert.That(Difference(average, texture.Average), Is.LessThan(0.001f));
	}

	[Test]
	public void Coherence([Random(0f, 1f, 4)] float x, [Random(0f, 1f, 4)] float y)
	{
		Distro2 distro = (Distro2)new Float2(x, y);

		Vector128<float> sampled = texture.Sample(distro, out Float3 incident, out float pdf);

		Assert.That(Difference(sampled, texture.Evaluate(incident)), Is.LessThan(0.001f));
		Assert.That(pdf, Is.EqualTo(texture.ProbabilityDensity(incident)).Roughly(5));
	}

	static ArrayGrid GenerateRandomTexture(Int2 maxSize)
	{
		IRandom random = Utilities.NewRandom();

		ArrayGrid texture = new ArrayGrid(new Int2(random.Next1(maxSize.x), random.Next1(maxSize.y)));
		texture.ForEach(position => texture[position] = Common.Utilities.ToVector(random.Next3()));

		return texture;
	}

	static Vector128<float> CalculateAverage(IDirectionalTexture texture, int sampleCount = 1000000)
	{
		IRandom random = Utilities.NewRandom();
		Vector128<float> sum = Vector128<float>.Zero;

		for (int i = 0; i < sampleCount; i++)
		{
			Float3 direction = random.NextOnSphere();
			sum = Sse.Add(sum, texture.Evaluate(direction));
		}

		return Sse.Divide(sum, Vector128.Create((float)sampleCount));
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