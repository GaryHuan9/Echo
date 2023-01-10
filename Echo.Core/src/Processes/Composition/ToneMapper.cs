using Echo.Core.Common.Compute.Async;
using Echo.Core.Common.Mathematics;
using Echo.Core.Common.Packed;
using Echo.Core.Textures.Colors;
using Echo.Core.Textures.Grids;

namespace Echo.Core.Processes.Composition;

public record ToneMapper : ICompositeLayer
{
	/// <summary>
	/// The label of the layer to operate on.
	/// </summary>
	public string TargetLayer { get; init; } = "main";

	/// <summary>
	/// The mode to use.
	/// </summary>
	public ILuminanceAdjuster Mode { get; init; } = new BasicShoulder();

	public ComputeTask ExecuteAsync(ICompositeContext context)
	{
		var sourceTexture = context.GetWriteTexture<RGB128>(TargetLayer);
		return context.RunAsync(MainPass, sourceTexture.size);

		void MainPass(Int2 position)
		{
			RGB128 source = sourceTexture[position];
			float luminance = source.Luminance;
			if (FastMath.AlmostZero(luminance)) return;

			RGB128 adjusted = source / luminance * Mode.Adjust(luminance);
			sourceTexture.Set(position, (RGB128)Float4.Min(adjusted, Float4.One));
		}
	}

	public interface ILuminanceAdjuster
	{
		public float Adjust(float luminance);
	}
}

/// <summary>
/// The ACES filmic luminance curve for <see cref="ToneMapper"/>.
/// Implementation based on https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
/// </summary>
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
/// A basic smooth shoulder luminance curve for <see cref="ToneMapper"/>.
/// Implementation based on https://www.desmos.com/calculator/nngw01x7om
/// </summary>
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
public class Reinhard : ToneMapper.ILuminanceAdjuster
{
	public Reinhard() => WhitePoint = 1.5f;

	readonly float _whitePoint;
	readonly float whitePoint2R;

	/// <summary>
	/// The smallest exposure adjusted luminance that should be mapped to 1.
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