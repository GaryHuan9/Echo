using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Textures.Directional;

[EchoSourceUsable]
public class CylindricalTexture : IDirectionalTexture
{
	DiscreteDistribution2D distribution;

	/// <summary>
	/// The Jacobian used to convert from uv coordinates to spherical coordinates.
	/// NOTE: we are also missing the one over sin phi term.
	/// </summary>
	const float Jacobian = Scalars.TauR / Scalars.Pi;

	NotNull<Texture> _texture = Pure.black;

	[EchoSourceUsable]
	public Texture Texture
	{
		get => _texture;
		set => _texture = value;
	}

	public RGB128 Average { get; private set; }

	public void Prepare()
	{
		//Fetch needed resources
		Texture texture = Texture;
		Int2 size = texture.DiscreteResolution;

		Ensure.IsTrue(size > Int2.Zero);
		Float2 sizeR = 1f / size;

		using var _ = Pool<float>.Fetch(size.Product, out View<float> weights);

		//Prepare for summing the weighted value of the texture
		var total = Summation.Zero;
		var locker = new object();

		//Loop through all horizontal rows
		Parallel.For(0, size.Y, () => Summation.Zero, (y, _, sum) =>
		{
			//Calculate sin weights and create fill for this horizontal row
			float sin0 = MathF.Sin(Scalars.Pi * (y + 0f) * sizeR.Y).Clamp();
			float sin1 = MathF.Sin(Scalars.Pi * (y + 1f) * sizeR.Y).Clamp();
			var fill = new SpanFill<float>(weights.AsSpan(y * size.X, size.X));

			//Loop horizontally, caching the sum of the two previous x colors
			var previous = Grab(0);

			for (int x = 1; x <= size.X; x++)
			{
				//Calculate the average of the four corners
				RGB128 current = Grab(x);
				RGB128 average = (previous + current) / 4f;

				//Accumulate horizontally
				previous = current;
				fill.Add(average.Luminance);
				sum += average;
			}

			Ensure.IsTrue(fill.IsFull);

			return sum;

			//Returns the sum of the values at (x, y) and (x, y + 1)
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			RGB128 Grab(int x)
			{
				var lower = (RGB128)texture[new Float2(x, y + 0f) * sizeR];
				var upper = (RGB128)texture[new Float2(x, y + 1f) * sizeR];
				return lower * sin0 + upper * sin1;
			}
		}, sum =>
		{
			//Accumulate vertically
			lock (locker) total += sum;
		});

		//Construct distribution and calculate average from total sum
		distribution = new DiscreteDistribution2D(weights, size.X);
		Average = (RGB128)(total.Result * Scalars.Pi / 2f / size.Product);
	}

	/// <inheritdoc/>
	public RGB128 Evaluate(Float3 incident) => (RGB128)Texture[ToUV(incident)];

	/// <inheritdoc/>
	public float ProbabilityDensity(Float3 incident)
	{
		Float2 uv = ToUV(incident);
		float cosP = -incident.Y;
		float sinP = FastMath.Identity(cosP);

		if (!FastMath.Positive(sinP)) return 0f;

		return distribution.ProbabilityDensity((Sample2D)uv) * Jacobian / sinP;
	}

	/// <inheritdoc/>
	public Probable<RGB128> Sample(Sample2D sample, out Float3 incident)
	{
		Probable<Sample2D> sampled = distribution.Sample(sample);

		if (sampled.NotPossible)
		{
			incident = Float3.Zero;
			return Probable<RGB128>.Impossible;
		}

		Float2 uv = sampled.content;

		float theta = uv.X * Scalars.Tau;
		float phi = uv.Y * Scalars.Pi;

		FastMath.SinCos(theta, out float sinT, out float cosT);
		FastMath.SinCos(phi, out float sinP, out float cosP);

		if (!FastMath.Positive(sinP))
		{
			incident = Float3.Zero;
			return Probable<RGB128>.Impossible;
		}

		incident = new Float3(-sinP * sinT, -cosP, -sinP * cosT);
		return ((RGB128)Texture[uv], sampled.pdf * Jacobian / sinP);
	}

	public static Float2 ToUV(Float3 direction)
	{
		Ensure.AreEqual(direction.SquaredMagnitude, 1f);

		return new Float2
		(
			FastMath.FMA(MathF.Atan2(direction.X, direction.Z), Scalars.TauR, 0.5f),
			FastMath.FMA(MathF.Acos(FastMath.Clamp11(direction.Y)), -Scalars.PiR, 1f)
		);
	}

	public static Float3 ToDirection(Float2 uv)
	{
		Ensure.IsTrue(uv.X is >= 0f and <= 1f);
		Ensure.IsTrue(uv.Y is >= 0f and <= 1f);

		float theta = uv.X * Scalars.Tau;
		float phi = uv.Y * Scalars.Pi;

		FastMath.SinCos(theta, out float sinT, out float cosT);
		FastMath.SinCos(phi, out float sinP, out float cosP);

		return new Float3(-sinP * sinT, -cosP, -sinP * cosT).Normalized;
	}
}