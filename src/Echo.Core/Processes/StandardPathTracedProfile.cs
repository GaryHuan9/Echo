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
/// A easier way of setting up a <see cref="RenderProfile"/> with for a standard path traced render.
/// </summary>
[EchoSourceUsable]
public record StandardPathTracedProfile : RenderProfile
{
	[EchoSourceUsable]
	public StandardPathTracedProfile(Scene scene, uint quality = 40, bool watermark = true)
	{
		Scene = scene;
		if (quality == 0) quality = 1;

		var builder0 = ImmutableArray.CreateBuilder<EvaluationProfile>();
		var builder1 = ImmutableArray.CreateBuilder<ICompositeLayer>();

		int extend = quality switch
		{
			> 800 => 1024,
			> 190 => 256,
			> 30 => 64,
			>= 0 => 16
		};

		int minEpoch = ((float)quality / extend * 2f).Round();

		var evaluation = new EvaluationProfile
		{
			Distribution = new StratifiedDistribution { Extend = extend },
			MaxEpoch = Math.Max(20, (MathF.Pow(quality, 2.1f) / extend / 10f).Round())
		};

		builder0.Add(evaluation with { NoiseThreshold = 0.9f / quality, Evaluator = new AlbedoEvaluator(), MinEpoch = 1, TargetLayer = "albedo" });
		builder0.Add(evaluation with { NoiseThreshold = 1.0f / quality, Evaluator = new PathTracedEvaluator(), MinEpoch = minEpoch, TargetLayer = "path" });
		builder0.Add(evaluation with { NoiseThreshold = 0.7f / quality, Evaluator = new NormalDepthEvaluator(), MinEpoch = 1, TargetLayer = "normal_depth" });

		builder1.Add(new TextureManage { CopySources = ImmutableArray.Create("path"), CopyLayers = ImmutableArray.Create("main") });
		builder1.Add(new OidnDenoise());
		builder1.Add(new AutoExposure());
		builder1.Add(new Vignette());
		builder1.Add(new Bloom());
		builder1.Add(new ToneMapper());
		builder1.Add(new Watermark { Enabled = watermark });

		EvaluationProfiles = builder0.ToImmutable();
		CompositionLayers = builder1.ToImmutable();
	}
}