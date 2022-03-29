using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using CodeHelpers.Mathematics;
using EchoRenderer.Common;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Scattering;
using EchoRenderer.Core.Texturing;

namespace EchoRenderer.Core.Rendering.Materials;

/// <summary>
/// A <see cref="Material"/> that is emissive, meaning that any surface it is applied to will contribute photons to the scene.
/// The tint is determined by the total average of <see cref="Material.Albedo"/>; currently textured area light is not supported.
/// </summary>
public class Emissive : Material, IEmissive
{
	/// <inheritdoc/>
	public float Power { get; private set; }

	Float3 emission;

	public override void Prepare()
	{
		base.Prepare();

		//Get averaged texture value
		Texture texture = Albedo;
		Int2 size = texture.DiscreteResolution;
		Float2 sizeR = 1f / size;

		var total = Summation.Zero;
		var locker = new object();

		Parallel.For(0, size.Product, () => Summation.Zero, (i, _, sum) =>
		{
			Int2 position = new Int2(i % size.x, i / size.x);
			return sum + texture[(position + Float2.half) * sizeR];
		}, sum =>
		{
			//Accumulate sums
			lock (locker) total += sum;
		});

		//Calculate average and power from total sum
		Vector128<float> average = Sse.Divide(total.Result, Vector128.Create((float)size.Product));

		emission = Utilities.ToFloat3(average);
		Power = PackedMath.GetLuminance(average) * Scalars.TAU;
	}

	public override void Scatter(ref Touch touch, Allocator allocator)
	{
		//Empty bsdf for zero scattering
		_ = new MakeBSDF(ref touch, allocator);
	}

	/// <inheritdoc/>
	public Float3 Emit(in GeometryPoint point, in Float3 outgoing) => emission;
}

/// <summary>
/// An interface that should be added to any <see cref="Material"/> that can directly create emission.
/// </summary>
public interface IEmissive
{
	/// <summary>
	/// Returns the approximated emitted power of this <see cref="IEmissive"/> per unit area.
	/// If this property returns a non-positive value, this entire interface can be ignored.
	/// </summary>
	public float Power { get; }

	/// <summary>
	/// Returns the emission of this <see cref="IEmissive"/> on a surface and leaving
	/// <paramref name="point"/>, towards the <paramref name="outgoing"/> direction.
	/// </summary>
	public Float3 Emit(in GeometryPoint point, in Float3 outgoing);
}