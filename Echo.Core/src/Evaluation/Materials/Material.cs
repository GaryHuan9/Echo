using Echo.Core.Aggregation.Primitives;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Mathematics.Primitives;
using Echo.Core.Common.Memory;
using Echo.Core.Common.Packed;
using Echo.Core.Evaluation.Scattering;
using Echo.Core.Textures;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Evaluation.Materials;

public abstract class Material
{
	NotNull<Texture> _albedo = Pure.white;
	NotNull<Texture> _normal = Pure.normal;

	/// <summary>
	/// The primary color of this <see cref="Material"/>.
	/// </summary>
	public Texture Albedo
	{
		get => _albedo;
		set => _albedo = value;
	}

	/// <summary>
	/// The local normal direction deviation of this <see cref="Material"/>.
	/// </summary>
	public Texture Normal
	{
		get => _normal;
		set => _normal = value;
	}

	/// <summary>
	/// The intensity of <see cref="Normal"/> on this <see cref="Material"/>.
	/// </summary>
	public float NormalIntensity { get; set; } = 1f;

	bool zeroNormal;

	/// <summary>
	/// Invoked before a new render session begins; can be used to execute any kind of preprocessing work for this <see cref="Material"/>.
	/// NOTE: invoking any of the rendering related methods prior to invoking this method after a change will result in undefined behaviors!
	/// </summary>
	public virtual void Prepare() => zeroNormal = Normal == Pure.normal || FastMath.AlmostZero(NormalIntensity);

	/// <summary>
	/// Determines the scattering properties of this material at <paramref name="contact"/>
	/// and potentially initializes the appropriate properties in <paramref name="contact"/>.
	/// </summary>
	public virtual void Scatter(ref Contact contact, Allocator allocator)
	{
		RGBA128 sampled = SampleAlbedo(contact);
		if (sampled.Alpha < 0.5f) return;

		var albedo = (RGB128)sampled;
		contact.bsdf = albedo.IsZero ?
			NewBSDF(contact, allocator, RGB128.Black) :
			Scatter(contact, allocator, albedo);
	}

	/// <inheritdoc cref="Scatter(ref Contact, Allocator"/>
	public abstract BSDF Scatter(in Contact contact, Allocator allocator, in RGB128 albedo);

	/// <summary>
	/// Applies this <see cref="Material"/>'s <see cref="Normal"/> mapping at <paramref name="texcoord"/>
	/// to <paramref name="normal"/>. Returns whether this method caused <paramref name="normal"/> to change.
	/// </summary>
	public bool ApplyNormalMapping(in Float2 texcoord, ref Float3 normal)
	{
		if (zeroNormal) return false;

		//Evaluate normal texture at texcoord
		Float4 local = Float4.Clamp(Normal[texcoord]);
		local = local * 2f - new Float4(1f, 1f, 2f, 0f); //OPTIMIZE fma

		local *= NormalIntensity;
		if (local == Float4.Zero) return false;

		//Create transform to move local direction to world-space
		NormalTransform transform = new NormalTransform(normal);
		Float3 delta = transform.LocalToWorld(local.XYZ);

		normal = (normal - delta).Normalized;
		return true;
	}

	/// <summary>
	/// Samples the <see cref="Albedo"/>.
	/// </summary>
	/// <param name="contact">The <see cref="Contact"/> at which we should sample the <see cref="Albedo"/>.</param>
	/// <returns>The sampled result.</returns>
	public RGBA128 SampleAlbedo(in Contact contact) => Albedo[contact.shade.Texcoord];

	/// <summary>
	/// Samples <paramref name="texture"/> at <paramref name="contact"/> and returns the resulting <see cref="RGB128"/>.
	/// </summary>
	protected static RGB128 Sample(Texture texture, in Contact contact) => (RGB128)texture[contact.shade.Texcoord];

	/// <summary>
	/// Creates a new <see cref="BSDF"/>
	/// </summary>
	protected static BSDF NewBSDF(in Contact contact, Allocator allocator, in RGB128 albedo)
	{
		var bsdf = allocator.New<BSDF>();
		bsdf.Reset(contact, albedo);
		return bsdf;
	}
}