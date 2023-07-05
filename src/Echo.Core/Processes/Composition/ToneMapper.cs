using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Textures.Colors;

namespace Echo.Core.Processes.Composition;

[EchoSourceUsable]
public record ToneMapper : ICompositeLayer
{
	/// <summary>
	/// The name of the layer to operate on.
	/// </summary>
	[EchoSourceUsable]
	public string LayerName { get; init; } = "main";

	/// <summary>
	/// Alters the saturation of each color, with 0 being completely desaturated and 1 being the original saturation.
	/// </summary>
	/// <remarks>This adjacent is performed prior to the tone mapping and this value is unclamped.</remarks>
	[EchoSourceUsable]
	public float Saturation { get; init; } = 1f;

	/// <summary>
	/// If true, the mapping effect consider each color as a whole. Otherwise, the mapping is done on each channel individually.
	/// </summary>
	[EchoSourceUsable]
	public bool CombineChannels { get; init; } = true;

	/// <summary>
	/// The mode to use.
	/// </summary>
	[EchoSourceUsable]
	public ILuminanceAdjuster Mode { get; init; } = new BasicShoulder();

	/// <inheritdoc/>
	[EchoSourceUsable]
	public bool Enabled { get; init; } = true;

	public async ComputeTask ExecuteAsync(ICompositeContext context)
	{
		var sourceTexture = context.GetWriteTexture<RGB128>(LayerName);

		await context.RunAsync(SaturationPass, sourceTexture.size);
		await context.RunAsync(CombineChannels ? CombinedPass : PerChannelPass, sourceTexture.size);

		void SaturationPass(Int2 position)
		{
			RGB128 source = sourceTexture.Get(position);
			Float4 gray = (Float4)source.Luminance;

			sourceTexture.Set(position, (RGB128)Float4.Lerp(gray, source, Saturation));
		}

		void CombinedPass(Int2 position)
		{
			RGB128 source = sourceTexture.Get(position);
			float luminance = source.Luminance;
			if (FastMath.AlmostZero(luminance)) return;

			RGB128 adjusted = source / luminance * Mode.Adjust(luminance);
			sourceTexture.Set(position, (RGB128)Float4.Min(adjusted, Float4.One));
		}

		void PerChannelPass(Int2 position)
		{
			RGB128 source = sourceTexture.Get(position);

			RGB128 adjusted = new RGB128
			(
				FastMath.AlmostZero(source.R) ? 0f : Mode.Adjust(source.R),
				FastMath.AlmostZero(source.G) ? 0f : Mode.Adjust(source.G),
				FastMath.AlmostZero(source.B) ? 0f : Mode.Adjust(source.B)
			);

			sourceTexture.Set(position, (RGB128)Float4.Min(adjusted, Float4.One));
		}
	}

	public interface ILuminanceAdjuster
	{
		public float Adjust(float luminance);
	}
}

/// <summary>
/// Performs no action on the luminance; this is a noop.
/// </summary>
[EchoSourceUsable]
public record PassThrough : ToneMapper.ILuminanceAdjuster
{
	public float Adjust(float luminance) => luminance;
}

/// <summary>
/// A basic smooth shoulder luminance curve for <see cref="ToneMapper"/>.
/// Implementation based on https://www.desmos.com/calculator/nngw01x7om
/// </summary>
[EchoSourceUsable]
public record BasicShoulder : ToneMapper.ILuminanceAdjuster
{
	public float Smoothness { get; init; } = 0.4f;

	public float Adjust(float luminance)
	{
		float oneLess = luminance - 1f;

		float h = (0.5f - 0.5f * oneLess / Smoothness).Clamp();
		return (oneLess - Smoothness + Smoothness * h) * h + 1f;
	}
}

/// <summary>
/// The Reinhard luminance curve for <see cref="ToneMapper"/>.
/// Implementation based on Photographic Tone Reproduction for Digital Images [Reinhard et al. 2002].
/// https://www.cs.utah.edu/docs/techreports/2002/pdf/UUCS-02-001.pdf
/// </summary>
[EchoSourceUsable]
public class Reinhard : ToneMapper.ILuminanceAdjuster
{
	public Reinhard() => WhitePoint = 1.5f;

	readonly float _whitePoint;
	readonly float whitePoint2R;

	/// <summary>
	/// The lowest luminance that should be mapped to 1.
	/// </summary>
	public float WhitePoint
	{
		get => _whitePoint;
		init
		{
			_whitePoint = value;
			whitePoint2R = 1f / (value * value);
		}
	}

	public float Adjust(float luminance)
	{
		float numerator = 1f + luminance * whitePoint2R;
		float denominator = 1f + luminance;

		return FastMath.Min(luminance * numerator / denominator, 1f);
	}
}

/// <summary>
/// The ACES filmic luminance curve for <see cref="ToneMapper"/>.
/// Implementation based on https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
/// </summary>
[EchoSourceUsable]
public record ACES : ToneMapper.ILuminanceAdjuster
{
	public float Adjust(float luminance)
	{
		float fma0 = FastMath.FMA(2.51f, luminance, 0.03f);
		float fma1 = FastMath.FMA(2.43f, luminance, 0.59f);

		return FastMath.Min(luminance * fma0 / (luminance * fma1 + 0.14f), 1f);
	}
}

/// <summary>
/// Popular video game filmic tone mapping curve by John Hable, used in Uncharted 2.
/// Implementation based on http://filmicworlds.com/blog/filmic-tonemapping-operators/
/// </summary>
/// <remarks>Curve shown here: https://www.desmos.com/calculator/tszlqcpog4</remarks>
[EchoSourceUsable]
public record Hable : ToneMapper.ILuminanceAdjuster
{
	public Hable() => WhitePoint = 11.2f;

	const float Scale = 16f;

	readonly float _whitePoint;
	readonly float multiplier;

	/// <summary>
	/// The lowest luminance that should be mapped to 1.
	/// </summary>
	public float WhitePoint
	{
		get => _whitePoint;
		init
		{
			_whitePoint = value;
			multiplier = 1f / Curve(value * Scale);
		}
	}

	public float Adjust(float luminance) => Curve(luminance * Scale) * multiplier;

	static float Curve(float value)
	{
		const float A = 0.15f;
		const float B = 0.50f;
		const float C = 0.10f;
		const float D = 0.20f;
		const float E = 0.02f;
		const float F = 0.30f;

		float numerator = FastMath.FMA(FastMath.FMA(value, A, C * B), value, D * E);
		float denominator = FastMath.FMA(FastMath.FMA(value, A, B), value, D * F);
		return numerator / denominator - E / F;
	}
}