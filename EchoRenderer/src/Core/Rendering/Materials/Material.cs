using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CodeHelpers;
using CodeHelpers.Packed;
using EchoRenderer.Common;
using EchoRenderer.Common.Coloring;
using EchoRenderer.Common.Mathematics;
using EchoRenderer.Common.Mathematics.Primitives;
using EchoRenderer.Common.Memory;
using EchoRenderer.Core.Aggregation.Primitives;
using EchoRenderer.Core.Rendering.Scattering;
using EchoRenderer.Core.Texturing;

namespace EchoRenderer.Core.Rendering.Materials;

public abstract class Material
{
	NotNull<Texture> _albedo = Texture.black;
	NotNull<Texture> _normal = Texture.normal;

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
	public virtual void Prepare() => zeroNormal = Normal == Texture.normal || FastMath.AlmostZero(NormalIntensity);

	/// <summary>
	/// Determines the scattering properties of this material at <paramref name="touch"/>
	/// and potentially initializes the appropriate properties in <paramref name="touch"/>.
	/// </summary>
	public abstract void Scatter(ref Touch touch, Allocator allocator);

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

		//Create transform to move local direction to world space
		NormalTransform transform = new NormalTransform(normal);
		Float3 delta = transform.LocalToWorld(local.XYZ);

		normal = (normal - delta).Normalized;
		return true;
	}

	/// <summary>
	/// Samples <see cref="Albedo"/> at <paramref name="touch"/> and returns the resulting <see cref="RGBA128"/>.
	/// </summary>
	public RGBA128 SampleAlbedo(in Touch touch) => Albedo[touch.shade.Texcoord];

	/// <summary>
	/// Samples <paramref name="texture"/> at <paramref name="touch"/> and returns the resulting <see cref="RGB128"/>.
	/// </summary>
	protected static RGB128 Sample(Texture texture, in Touch touch) => (RGB128)texture[touch.shade.Texcoord];

	/// <summary>
	/// A wrapper struct used to easily create <see cref="BSDF"/> and add <see cref="BxDF"/> to it.
	/// </summary>
	protected readonly ref struct MakeBSDF
	{
		public MakeBSDF(ref Touch touch, Allocator allocator)
		{
			this.allocator = allocator;
			bsdf = allocator.New<BSDF>();

			touch.bsdf = bsdf;
			bsdf.Reset(touch);
		}

		readonly Allocator allocator;
		readonly BSDF bsdf;

		/// <summary>
		/// Adds a new <see cref="BxDF"/> of type <typeparamref name="T"/> to <see cref="Touch.bsdf"/> and returns it.
		/// </summary>
		public T Add<T>() where T : BxDF, new()
		{
			T function = allocator.New<T>();

			bsdf.Add(function);
			return function;
		}
	}
}