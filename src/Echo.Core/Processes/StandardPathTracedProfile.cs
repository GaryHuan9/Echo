using System;
using System.Collections.Immutable;
using Echo.Core.Common.Mathematics;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Evaluation.Sampling;
using Echo.Core.InOut.EchoDescription;
using Echo.Core.Processes.Composition;
using Echo.Core.Processes.Evaluation;
using Echo.Core.Scenic.Hierarchies;

namespace Echo.Core.Processes;

/// <summary>
/// A easier way of setting up a <see cref="RenderProfile"/> for a standard path traced render.
/// </summary>
[EchoSourceUsable]
public record StandardPathTracedProfile : RenderProfile
{
	[EchoSourceUsable]
	public StandardPathTracedProfile(Scene scene, uint quality = 40)
	{
		Scene = scene;
		if (quality == 0) quality = 1;

		var evaluations = ImmutableArray.CreateBuilder<EvaluationProfile>();

		int extend = quality switch
		{
			> 800 => 1024,
			> 190 => 256,
			> 30 => 64,
			_ => 16
		};

		int minEpoch = ((float)quality / extend * 2f).Round();

		var template = new EvaluationProfile
		{
			Distribution = new StratifiedDistribution { Extend = extend },
			MaxEpoch = Math.Max(20, (MathF.Pow(quality, 2.1f) / extend / 10f).Round())
		};

		evaluations.Add(template with { NoiseThreshold = 0.9f / quality, Evaluator = new AlbedoEvaluator(), MinEpoch = 1, LayerName = "albedo" });
		evaluations.Add(template with { NoiseThreshold = 1.0f / quality, Evaluator = new PathTracedEvaluator(), MinEpoch = minEpoch, LayerName = "path" });
		evaluations.Add(template with { NoiseThreshold = 0.7f / quality, Evaluator = new NormalDepthEvaluator(), MinEpoch = 1, LayerName = "normal_depth" });

		EvaluationProfiles = evaluations.ToImmutable();
		CompositionLayers = CreateCompositionLayers();
	}

	readonly bool _onlyDenoise;
	readonly bool _watermark = true;

	/// <summary>
	/// Whether the composition process performs only denoise and nothing else.
	/// </summary>
	[EchoSourceUsable]
	public bool OnlyDenoise
	{
		get => _onlyDenoise;
		init
		{
			if (_onlyDenoise == value) return;
			_onlyDenoise = value;
			CompositionLayers = CreateCompositionLayers();
		}
	}

	/// <summary>
	/// Whether to include the <see cref="Echo"/> watermark on the final output.
	/// </summary>
	/// <remarks>This option ignores <see cref="OnlyDenoise"/>.</remarks>
	[EchoSourceUsable]
	public bool Watermark
	{
		get => _watermark;
		init
		{
			if (_watermark == value) return;
			_watermark = value;
			CompositionLayers = CreateCompositionLayers();
		}
	}

	ImmutableArray<ICompositeLayer> CreateCompositionLayers() => CreateCompositionLayers(OnlyDenoise, Watermark);

	static ImmutableArray<ICompositeLayer> CreateCompositionLayers(bool onlyDenoise, bool watermark)
	{
		var builder = ImmutableArray.CreateBuilder<ICompositeLayer>();

		builder.Add(new TextureManage { CopySources = ImmutableArray.Create("path"), CopyLayers = ImmutableArray.Create("main") });
		builder.Add(new OidnDenoise());

		if (!onlyDenoise)
		{
			builder.Add(new AutoExposure());
			builder.Add(new Vignette());
			builder.Add(new Bloom());
			builder.Add(new ToneMapper { LimitToOne = true });
		}

		builder.Add(new Watermark { Enabled = watermark });

		return builder.ToImmutable();
	}
}