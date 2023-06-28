using System;
using System.Threading.Tasks;
using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

/// <summary>
/// A <see cref="Material"/> that is emissive, meaning that any surface it is applied to will contribute radiance to the scene.
/// The color is determined by the total average of <see cref="Material.Albedo"/>; currently textured area light is not supported.
/// </summary>
[EchoSourceUsable]
public sealed class Emissive : Material
{
	/// <summary>
	/// Returns the approximated emitted power of this <see cref="Emissive"/> per unit area.
	/// If this property returns a non-positive value, this entire interface can be ignored.
	/// </summary>
	public float Power { get; private set; }

	RGB128 emission;

	public override void Prepare()
	{
		base.Prepare();

		//Get averaged texture value
		Texture texture = Albedo;
		Int2 size = texture.DiscreteResolution;

		var total = Summation.Zero;
		var locker = new object();

		Parallel.For(0, size.Y, () => Summation.Zero, (y, _, sum) =>
		{
			for (int x = 0; x < size.X; x++) sum += texture[new Int2(x, y)];
			return sum;
		}, sum =>
		{
			//Accumulate sums
			lock (locker) total += sum;
		});

		//Calculate emission and power from total sum
		emission = (RGB128)total.Result / size.Product;
		Power = emission.Luminance * Scalars.Pi;
	}

	public override void Scatter(ref Contact contact, Allocator allocator) => contact.bsdf = NewBSDF(contact, allocator, RGB128.Black);
	protected override BSDF Scatter(in Contact contact, Allocator allocator, RGB128 albedo) => throw new NotSupportedException();

	/// <summary>
	/// Returns the emission of this <see cref="Emissive"/>.
	/// </summary>
	/// <param name="point">A <see cref="GeometryPoint"/> on this <see cref="Emissive"/> surface.</param>
	/// <param name="outgoing">A direction leaving this <see cref="Emissive"/> surface from <paramref name="point"/>.</param>
	public RGB128 Emit(in GeometryPoint point, Float3 outgoing) => outgoing.Dot(point.normal) > 0f ? emission : RGB128.Black;
}