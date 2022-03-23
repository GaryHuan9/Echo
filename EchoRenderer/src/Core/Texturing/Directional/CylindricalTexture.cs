using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using CodeHelpers;
using CodeHelpers.Diagnostics;
using CodeHelpers.Mathematics;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Rendering.Distributions;
using EchoRenderer.Core.Rendering.Distributions.Discrete;

namespace EchoRenderer.Core.Texturing.Directional;

public class CylindricalTexture : IDirectionalTexture
{
	NotNull<Texture> _texture = Texture.black;

	public Texture Texture
	{
		get => _texture;
		set => _texture = value;
	}

	DiscreteDistribution2D distribution;

	/// <summary>
	/// The Jacobian used to convert from uv coordinates to spherical coordinates.
	/// NOTE: we are also missing the one over sin phi term.
	/// </summary>
	const float Jacobian = 1f / Scalars.TAU / Scalars.PI;

	public Vector128<float> Average { get; private set; }

	public void Prepare()
	{
		//Fetch needed resources
		Texture texture = Texture;
		Int2 size = texture.DiscreteResolution;

		Assert.IsTrue(size > Int2.zero);
		Float2 sizeR = 1f / size;

		using var _ = Pool<float>.Fetch(size.Product, out View<float> weights);

		//Prepare for summing the weighted value of the texture
		var total = Summation.Zero;
		var locker = new object();

		//Loop through all horizontal rows
		Parallel.For(0, size.y, () => Summation.Zero, (y, _, sum) =>
		{
			//Calculate sin weights and create fill for this horizontal row
			float sin0 = MathF.Sin(Scalars.PI * (y + 0f) * sizeR.y);
			float sin1 = MathF.Sin(Scalars.PI * (y + 1f) * sizeR.y);
			SpanFill<float> fill = weights.AsSpan(y * size.x, size.x);

			//Loop horizontally, caching the sum of the two previous x colors
			var previous = Grab(0);

			for (int x = 1; x <= size.x; x++)
			{
				var current = Grab(x);

				//Calculate the average of the four corners
				var average = Sse.Add(previous, current);
				average = Sse.Multiply(average, Vector128.Create(1f / 4f));

				//Accumulate horizontally
				previous = current;
				fill.Add(PackedMath.GetLuminance(average));
				sum += average;
			}

			Assert.IsTrue(fill.IsFull);

			return sum;

			//Returns the sum of the values at (x, y) and (x, y + 1)
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			Vector128<float> Grab(int x)
			{
				var lower = texture[new Float2(x, y + 0f) * sizeR];
				var upper = texture[new Float2(x, y + 1f) * sizeR];

				lower = Sse.Multiply(lower, Vector128.Create(sin0));
				upper = Sse.Multiply(upper, Vector128.Create(sin1));

				return Sse.Add(lower, upper);
			}
		}, sum =>
		{
			//Accumulate vertically
			lock (locker) total += sum;
		});

		//Construct distribution and calculate average from total sum
		float weight = 2f / Scalars.PI / size.Product;
		distribution = new DiscreteDistribution2D(weights, size.x);
		Average = Sse.Multiply(total.Result, Vector128.Create(weight));
	}

	/// <inheritdoc/>
	public Vector128<float> Evaluate(in Float3 direction) => Texture[ToUV(direction)];

	/// <inheritdoc/>
	public Vector128<float> Sample(Sample2D sample, out Float3 incident, out float pdf)
	{
		Float2 uv = distribution.Sample(sample, out pdf);

		if (!FastMath.Positive(pdf))
		{
			incident = default;
			return Vector128<float>.Zero;
		}

		float angle0 = uv.x * Scalars.TAU;
		float angle1 = uv.y * Scalars.PI;

		FastMath.SinCos(angle0, out float sinT, out float cosT); //Theta
		FastMath.SinCos(angle1, out float sinP, out float cosP); //Phi

		incident = new Float3(-sinP * sinT, -cosP, -sinP * cosT);

		if (sinP <= 0f) pdf = 0f;
		else pdf *= Jacobian / sinP;

		return Texture[uv];
	}

	/// <inheritdoc/>
	public float ProbabilityDensity(in Float3 incident)
	{
		Float2 uv = ToUV(incident);
		float cosP = -incident.y;
		float sinP = FastMath.Identity(cosP);

		if (sinP <= 0f) return 0f;

		return distribution.ProbabilityDensity((Sample2D)uv) * Jacobian / sinP;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static Float2 ToUV(in Float3 direction) => new
	(
		FastMath.FMA(MathF.Atan2(direction.x, direction.z), 0.5f / Scalars.PI, 0.5f),
		FastMath.FMA(MathF.Acos(FastMath.Clamp11(direction.y)), -1f / Scalars.PI, 1f)
	);
}