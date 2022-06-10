using System;
using CodeHelpers.Packed;
using Echo.Common.Compute;
using Echo.Core.Evaluation.Distributions.Continuous;
using Echo.Core.Evaluation.Evaluators;
using Echo.Core.Evaluation.Operations;
using Echo.Core.Scenic.Examples;
using Echo.Core.Scenic.Preparation;
using Echo.Core.Textures.Evaluation;
using Echo.Core.Textures.Grid;
using Echo.Terminal.Application;
using Echo.Terminal.Application.Report;
using Echo.Terminal.Core;
using Echo.Terminal.Core.Interface;

namespace Echo.Terminal;

public class EchoTI : RootTI
{
	public EchoTI()
	{
		Child0 = new CommandTI();
		Child1 = new BisectionTI
		{
			Horizontal = true,
			Balance = 0.48f,

			Child0 = new TileReportTI(),
			Child1 = new BisectionTI
			{
				Horizontal = true,
				Balance = 0.55f,

				Child0 = new OperationReportTI(),
				Child1 = new DeviceReportTI()
			}
		};

		Balance = 0.16f;

		device = Device.Create();
	}

	readonly Device device;

	public override void ProcessArguments(string[] arguments)
	{
		base.ProcessArguments(arguments);

		var scene = new SingleBunny();

		var prepareProfile = new ScenePrepareProfile();

		var evaluationProfile = new EvaluationProfile
		{
			Scene = new PreparedScene(scene, prepareProfile),
			Evaluator = new PathTracedEvaluator(),
			Distribution = new StratifiedDistribution { Extend = 64 },
			Buffer = new RenderBuffer(new Int2(960, 540)),
			MinEpoch = 1,
			MaxEpoch = 1
		};

		var factory = new EvaluationOperation.Factory
		{
			NextProfile = evaluationProfile
		};

		device.Dispatch(factory);
	}

	// public override void Update(in Moment moment)
	// {
	// 	base.Update(in moment);
	//
	// 	while (Console.KeyAvailable)
	// 	{
	// 		ConsoleKeyInfo key = Console.ReadKey(true);
	// 		if (key.KeyChar == 'p') device.Pause();
	// 		if (key.KeyChar == 'r') device.Resume();
	// 	}
	// }

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing) device?.Dispose();
	}
}