using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Memory;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

public class Glass : Material
{
	NotNull<Texture> _refractiveIndex = Pure.white;

	public Texture RefractiveIndex
	{
		get => _refractiveIndex;
		set => _refractiveIndex = value;
	}

	public override void Scatter(ref Contact contact, Allocator allocator)
	{
		var make = new MakeBSDF(ref contact, allocator);

		var albedo = (RGB128)SampleAlbedo(contact);
		if (albedo.IsZero) return;

		float index = FastMath.Max0(Sample(RefractiveIndex, contact).R);

		make.Add<SpecularFresnel>().Reset(albedo, 1f, index);
	}
}