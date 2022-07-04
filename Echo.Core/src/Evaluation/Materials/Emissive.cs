using System.Threading.Tasks;
using CodeHelpers.Mathematics;
using CodeHelpers.Packed;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

/// <summary>
/// An interface that should be added to any <see cref="Material"/> that can directly create emission.
/// </summary>
public interface IEmissive
{
	/// <summary>
	/// Returns the approximated emitted power of this <see cref="IEmissive"/> per unit area.
	/// If this property returns a non-positive value, this entire interface can be ignored.
	/// </summary>
	float Power { get; }

	/// <summary>
	/// Returns the emission of this <see cref="IEmissive"/>.
	/// </summary>
	/// <param name="point">A <see cref="GeometryPoint"/> on this <see cref="IEmissive"/> surface.</param>
	/// <param name="outgoing">A direction leaving this <see cref="IEmissive"/> surface from <paramref name="point"/>.</param>
	RGB128 Emit(in GeometryPoint point, in Float3 outgoing);
}

/// <summary>
/// A <see cref="Material"/> that is emissive, meaning that any surface it is applied to will contribute photons to the scene.
/// The tint is determined by the total average of <see cref="Material.Albedo"/>; currently textured area light is not supported.
/// </summary>
public class Emissive : Material, IEmissive
{
	/// <inheritdoc/>
	public float Power { get; private set; }

	RGB128 emission;

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
			Int2 position = new Int2(i % size.X, i / size.X);
			return sum + texture[(position + Float2.Half) * sizeR];
		}, sum =>
		{
			//Accumulate sums
			lock (locker) total += sum;
		});

		//Calculate emission and power from total sum
		emission = (RGB128)total.Result / size.Product;
		Power = emission.Luminance * Scalars.Tau; //NOTE: multiply by 2 Pi here because we still assume that an emissive surface is two sided
	}

	public override void Scatter(ref Contact contact, Allocator allocator)
	{
		//Empty bsdf for zero scattering
		_ = new MakeBSDF(ref contact, allocator);
	}

	/// <inheritdoc/>
	public RGB128 Emit(in GeometryPoint point, in Float3 outgoing) => outgoing.Dot(point.normal) > 0f ? emission : RGB128.Black;
}